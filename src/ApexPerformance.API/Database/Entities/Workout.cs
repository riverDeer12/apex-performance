using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class Workout : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ThumbnailUrl { get; set; }
    public string VideoUrl { get; set; }
    // Comma separated codes of WorkoutMuscles; null until set or guessed.
    public string? Muscles { get; set; }
    // Muscles that help, same codes; null until set or guessed.
    public string? SecondaryMuscles { get; set; }
    // Done for a time (e.g. plank): sets record how long it lasted instead of repetitions.
    public bool IsTimed { get; set; }
    // WorkoutCategories: mobility and flexibility or strength and conditioning.
    public string Category { get; set; } = Constants.WorkoutCategories.StrengthConditioning;
    public ICollection<WorkoutWorkoutType> WorkoutTypes { get; set; } = null!;
    
}