using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Training that a coach prepares for a client,
/// with exercises from the workouts catalog.
/// </summary>
public class Training : BaseEntity
{
    public required string Name { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Note { get; set; }

    // Only coaches and administrators mark trainings as completed.
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // When the training really started and ended, marked in the apps.
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    // Client's heart rate between start and end, in beats per minute,
    // read from Apple Health on the client's phone.
    public int? HeartRateMin { get; set; }
    public int? HeartRateMax { get; set; }
    public int? HeartRateAverage { get; set; }

    public required Client Client { get; set; }
    public Guid ClientId { get; set; }

    public ICollection<TrainingExercise> Exercises { get; set; } = new List<TrainingExercise>();
}
