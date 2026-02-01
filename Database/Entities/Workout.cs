using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class Workout : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ThumbnailUrl { get; set; }
    public string VideoUrl { get; set; }
    public required WorkoutType WorkoutType { get; set; }
    public Guid WorkoutTypeId { get; set; }
}