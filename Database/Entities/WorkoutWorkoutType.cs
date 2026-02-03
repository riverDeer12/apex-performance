using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class WorkoutWorkoutType
{
    public Guid WorkoutId { get; set; }

    public Workout Workout { get; set; } = null!;

    public Guid WorkoutTypeId { get; set; }

    public WorkoutType WorkoutType { get; set; } = null!;
}