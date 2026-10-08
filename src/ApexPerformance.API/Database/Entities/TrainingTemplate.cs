using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Training prepared once and reused, coaches
/// assign it to clients as their trainings.
/// </summary>
public class TrainingTemplate : BaseEntity
{
    public required string Name { get; set; }
    public string? Note { get; set; }

    public ICollection<TrainingTemplateExercise> Exercises { get; set; } = new List<TrainingTemplateExercise>();
}

public class TrainingTemplateExercise
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid TrainingTemplateId { get; set; }
    public TrainingTemplate TrainingTemplate { get; set; } = null!;

    public Guid WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;

    public int Order { get; set; }
    public string? Note { get; set; }
    public bool IsSupersetWithPrevious { get; set; }

    public ICollection<TrainingTemplateExerciseSet> Sets { get; set; } = new List<TrainingTemplateExerciseSet>();
}

public class TrainingTemplateExerciseSet
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid TrainingTemplateExerciseId { get; set; }
    public TrainingTemplateExercise TrainingTemplateExercise { get; set; } = null!;

    public int Order { get; set; }
    public string? Reps { get; set; }
    public decimal? Weight { get; set; }
}
