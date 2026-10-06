using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Exercise of a training. Rows are replaced
/// together with the training when it is updated.
/// </summary>
public class TrainingExercise
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid TrainingId { get; set; }
    public Training Training { get; set; } = null!;

    public Guid WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;

    public int Order { get; set; }
    public string? Note { get; set; }

    public ICollection<TrainingExerciseSet> Sets { get; set; } = new List<TrainingExerciseSet>();
}
