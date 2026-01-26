using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class MeasurementUnit : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Symbol { get; set; } = null!;
}