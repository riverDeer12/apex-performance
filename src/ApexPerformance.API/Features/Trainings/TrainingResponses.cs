using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Shared.DataTransferObjects;

namespace ApexPerformance.API.Features.Trainings;

public record TrainingExerciseResponse(
    Guid Id,
    Guid WorkoutId,
    // Persisted JSON with workout name translations.
    string WorkoutName,
    int Order,
    int? Sets,
    string? Reps,
    decimal? Weight,
    string? Note
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

public record TrainingExerciseRequest(
    Guid Workout,
    int? Sets,
    string? Reps,
    decimal? Weight,
    string? Note
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
            .Select(x => new TrainingExerciseResponse(x.Id, x.WorkoutId, x.Workout.Name, x.Order, x.Sets,
                x.Reps, x.Weight, x.Note))
            .ToList(),
        training.CreatedAt,
        training.UpdatedAt);

    /// <summary>
    /// Exercises in the order they are given.
    /// </summary>
    public static List<TrainingExercise> ToExercises(IEnumerable<TrainingExerciseRequest>? exercises)
        => (exercises ?? []).Select((x, index) => new TrainingExercise
        {
            WorkoutId = x.Workout,
            Order = index,
            Sets = x.Sets,
            Reps = string.IsNullOrWhiteSpace(x.Reps) ? null : x.Reps.Trim(),
            Weight = x.Weight,
            Note = string.IsNullOrWhiteSpace(x.Note) ? null : x.Note.Trim()
        }).ToList();
}
