using System.Globalization;
using System.Text.RegularExpressions;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services.Implementation;

public partial class PersonalRecordService : IPersonalRecordService
{
    private readonly ApexPerformanceContext _context;
    private readonly ILogger<PersonalRecordService> _logger;

    public PersonalRecordService(ApexPerformanceContext context, ILogger<PersonalRecordService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecalculateForClientsAsync(IEnumerable<Guid> clientIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var clientId in clientIds.Distinct())
        {
            try
            {
                await RecalculateForClientAsync(clientId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recalculating personal records of client {ClientId} failed.", clientId);
            }
        }
    }

    public async Task CalculateMissingRecordsAsync()
    {
        var completed = _context.Trainings.Where(x => x.IsCompleted);

        // Records came after trainings: all clients. Durations came after records: clients
        // with timed sets and no duration record yet. Both happen only once.
        var clientIds = await _context.PersonalRecords.AnyAsync()
            ? await completed
                .Where(x => x.Exercises.Any(exercise => exercise.Sets.Any(set => set.DurationSeconds > 0)))
                .Where(x => !_context.PersonalRecords.Any(record =>
                    record.ClientId == x.ClientId && record.Type == PersonalRecordTypes.LongestDuration))
                .Select(x => x.ClientId)
                .Distinct()
                .ToListAsync()
            : await completed
                .Select(x => x.ClientId)
                .Distinct()
                .ToListAsync();

        await RecalculateForClientsAsync(clientIds);
    }

    private async Task RecalculateForClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var trainings = await _context.Trainings
            .AsNoTracking()
            .Where(x => x.ClientId == clientId && x.IsCompleted)
            .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var records = CalculateRecords(clientId, trainings);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.PersonalRecords
            .Where(x => x.ClientId == clientId)
            .ExecuteDeleteAsync(cancellationToken);

        _context.PersonalRecords.AddRange(records);

        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Goes through trainings in order and adds a record
    /// every time the best value so far is beaten.
    /// </summary>
    public static List<PersonalRecord> CalculateRecords(Guid clientId, IEnumerable<Training> trainings)
    {
        var best = new Dictionary<(Guid WorkoutId, string Type), decimal>();
        var records = new List<PersonalRecord>();

        foreach (var training in trainings)
        {
            // Sets over a distance (e.g. rowing) count by how far and how fast, not how long.
            var distanceSets = training.Exercises
                .SelectMany(exercise => exercise.Sets.Select(set => (exercise.WorkoutId, Set: set)))
                .Where(x => x.Set.DistanceMeters is > 0)
                .ToList();

            foreach (var workoutSets in distanceSets.GroupBy(x => x.WorkoutId))
            {
                var farthest = workoutSets.MaxBy(x => x.Set.DistanceMeters)!.Set;
                AddRecordIfBetter(workoutSets.Key, PersonalRecordTypes.LongestDistance, farthest.DistanceMeters!.Value,
                    0, null);

                var paced = workoutSets.Where(x => x.Set.DurationSeconds is > 0).ToList();

                if (paced.Count > 0)
                {
                    var fastest = paced.MaxBy(x => Speed(x.Set))!.Set;
                    AddRecordIfBetter(workoutSets.Key, PersonalRecordTypes.FastestPace, Math.Round(Speed(fastest), 2),
                        0, null);
                }
            }

            // Sets done for a time (e.g. plank) count by how long they lasted, with or without weight.
            var timedSets = training.Exercises
                .SelectMany(exercise => exercise.Sets.Select(set => (exercise.WorkoutId, Set: set)))
                .Where(x => x.Set.DurationSeconds is > 0 && x.Set.DistanceMeters is null or 0)
                .ToList();

            foreach (var workoutSets in timedSets.GroupBy(x => x.WorkoutId))
            {
                var longest = workoutSets.MaxBy(x => x.Set.DurationSeconds)!.Set;
                AddRecordIfBetter(workoutSets.Key, PersonalRecordTypes.LongestDuration, longest.DurationSeconds!.Value,
                    longest.Weight ?? 0, ParseReps(longest.Reps));
            }

            var sets = training.Exercises
                .SelectMany(exercise => exercise.Sets.Select(set => (exercise.WorkoutId, Set: set)))
                // Timed sets have their own record, their weight isn't a lifting record.
                .Where(x => x.Set.Weight is > 0 && x.Set.DurationSeconds is null or 0)
                .Select(x => (x.WorkoutId, Weight: x.Set.Weight!.Value, Reps: ParseReps(x.Set.Reps),
                    HasReps: !string.IsNullOrWhiteSpace(x.Set.Reps)))
                // Sets with text that is not repetitions (like "30 s") are not counted.
                .Where(x => !x.HasReps || x.Reps is >= 1)
                .ToList();

            foreach (var workoutSets in sets.GroupBy(x => x.WorkoutId))
            {
                var heaviest = workoutSets.MaxBy(x => x.Weight);
                AddRecordIfBetter(workoutSets.Key, PersonalRecordTypes.MaxWeight, heaviest.Weight, heaviest.Weight,
                    heaviest.Reps);

                var strongest = workoutSets.MaxBy(x => EstimatedOneRepMax(x.Weight, x.Reps));
                AddRecordIfBetter(workoutSets.Key, PersonalRecordTypes.EstimatedOneRepMax,
                    Math.Round(EstimatedOneRepMax(strongest.Weight, strongest.Reps), 2),
                    strongest.Weight, strongest.Reps);
            }

            void AddRecordIfBetter(Guid workoutId, string type, decimal value, decimal weight, decimal? reps)
            {
                var key = (workoutId, type);

                if (best.TryGetValue(key, out var current) && value <= current)
                    return;

                best[key] = value;

                records.Add(new PersonalRecord
                {
                    ClientId = clientId,
                    WorkoutId = workoutId,
                    TrainingId = training.Id,
                    Type = type,
                    Value = value,
                    Weight = weight,
                    Reps = reps,
                    AchievedAt = training.Date
                });
            }
        }

        return records;
    }

    // Average speed of a set over a distance, in metres per minute.
    private static decimal Speed(TrainingExerciseSet set)
        => set.DistanceMeters!.Value * 60m / set.DurationSeconds!.Value;

    // Epley formula, same as the progress charts in the web application.
    private static decimal EstimatedOneRepMax(decimal weight, decimal? reps)
        => weight * (1 + (reps ?? 1) / 30m);

    /// <summary>
    /// Reps are free text: "10" or a range "8-12" (its middle is used),
    /// same as in the progress charts. Anything else is not repetitions.
    /// </summary>
    public static decimal? ParseReps(string? value)
    {
        var match = RepsRegex().Match(value ?? string.Empty);

        if (!match.Success)
            return null;

        var from = decimal.Parse(match.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        var to = match.Groups[2].Success
            ? decimal.Parse(match.Groups[2].Value.Replace(',', '.'), CultureInfo.InvariantCulture)
            : from;

        return (from + to) / 2;
    }

    [GeneratedRegex(@"^\s*(\d+(?:[.,]\d+)?)\s*(?:-\s*(\d+(?:[.,]\d+)?))?\s*$")]
    private static partial Regex RepsRegex();
}
