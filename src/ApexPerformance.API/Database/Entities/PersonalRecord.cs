using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Client's record in an exercise, set in a completed training.
/// Every time a record is beaten a new row is added, so rows of
/// a client and exercise are the history of the record. Rows are
/// recalculated from completed trainings whenever they change.
/// </summary>
public class PersonalRecord
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public Guid WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;

    public Guid TrainingId { get; set; }
    public Training Training { get; set; } = null!;

    // One of PersonalRecordTypes.
    public required string Type { get; set; }

    // Weight for max weight, estimated one rep max for the other type.
    public decimal Value { get; set; }

    // The set the record was set with.
    public decimal Weight { get; set; }
    public decimal? Reps { get; set; }

    public DateTimeOffset AchievedAt { get; set; }
}
