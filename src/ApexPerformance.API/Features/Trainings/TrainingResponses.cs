using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Shared.DataTransferObjects;

namespace ApexPerformance.API.Features.Trainings;

public record TrainingExerciseSetResponse(
    Guid Id,
    int Order,
    string? Reps,
    decimal? Weight,
    // Done right after the previous set (drop set).
    bool IsDropSet,
    // How long the set lasted, in seconds (e.g. plank).
    int? DurationSeconds
);

public record TrainingExerciseResponse(
    Guid Id,
    Guid WorkoutId,
    // Persisted JSON with workout name translations.
    string WorkoutName,
    int Order,
    string? Note,
    List<TrainingExerciseSetResponse> Sets,
    bool IsSupersetWithPrevious
);

public record TrainingResponse(
    Guid Id,
    string Name,
    DateTimeOffset Date,
    string? Note,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    PersonDataDto Client,
    List<TrainingExerciseResponse> Exercises,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record TrainingExerciseSetRequest(
    string? Reps,
    decimal? Weight,
    // Optional, set is done right after the previous one (drop set).
    bool IsDropSet = false,
    // Optional, how long the set lasted in seconds (e.g. plank).
    int? DurationSeconds = null
);

public record TrainingExerciseRequest(
    Guid Workout,
    string? Note,
    List<TrainingExerciseSetRequest>? Sets,
    // Optional, exercise is done right after the previous one (superset).
    bool IsSupersetWithPrevious = false
);

public static class TrainingMapper
{
    public static TrainingResponse ToResponse(Training training) => new(
        training.Id,
        training.Name,
        training.Date,
        training.Note,
        training.IsCompleted,
        training.CompletedAt,
        new PersonDataDto(training.Client.Id, training.Client.FirstName, training.Client.LastName,
            training.Client.FullName),
        training.Exercises
            .OrderBy(x => x.Order)
            .Select(x => new TrainingExerciseResponse(x.Id, x.WorkoutId, x.Workout.Name, x.Order, x.Note,
                x.Sets
                    .OrderBy(set => set.Order)
                    .Select(set => new TrainingExerciseSetResponse(set.Id, set.Order, set.Reps, set.Weight,
                        set.IsDropSet, set.DurationSeconds))
                    .ToList(),
                x.IsSupersetWithPrevious))
            .ToList(),
        training.CreatedAt,
        training.UpdatedAt);

    /// <summary>
    /// Exercises and their sets in the order they are given.
    /// </summary>
    public static List<TrainingExercise> ToExercises(IEnumerable<TrainingExerciseRequest>? exercises)
        => (exercises ?? []).Select((x, index) => new TrainingExercise
        {
            WorkoutId = x.Workout,
            Order = index,
            Note = string.IsNullOrWhiteSpace(x.Note) ? null : x.Note.Trim(),
            // First exercise has no previous one to be in a superset with.
            IsSupersetWithPrevious = index > 0 && x.IsSupersetWithPrevious,
            Sets = (x.Sets ?? []).Select((set, setIndex) => new TrainingExerciseSet
            {
                Order = setIndex,
                Reps = string.IsNullOrWhiteSpace(set.Reps) ? null : set.Reps.Trim(),
                Weight = set.Weight,
                // First set has no previous one to drop from.
                IsDropSet = setIndex > 0 && set.IsDropSet,
                DurationSeconds = set.DurationSeconds
            }).ToList()
        }).ToList();
}
