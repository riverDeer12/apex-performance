using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// One set of a training exercise, each set
/// can have its own repetitions and weight.
/// </summary>
public class TrainingExerciseSet
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid TrainingExerciseId { get; set; }
    public TrainingExercise TrainingExercise { get; set; } = null!;

    public int Order { get; set; }

    // Text, so ranges like "8-12" or "30 s" can be used.
    public string? Reps { get; set; }

    public decimal? Weight { get; set; }
}
