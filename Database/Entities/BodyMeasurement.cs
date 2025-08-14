using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class BodyMeasurement : BaseEntity
{
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public decimal Shoulders { get; set; }
    public decimal Chest { get; set; }
    public decimal UpperArm { get; set; }
    public decimal Waist { get; set; }
    public decimal Thigh { get; set; }
    public decimal Calves { get; set; }
    public decimal Glutes { get; set; }

    public required Client Client { get; set; }
    
    public Guid ClientId { get; set; }
}