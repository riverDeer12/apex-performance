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

    public required Client Client { get; set; }
    public Guid ClientId { get; set; }

    public ICollection<TrainingExercise> Exercises { get; set; } = new List<TrainingExercise>();
}
