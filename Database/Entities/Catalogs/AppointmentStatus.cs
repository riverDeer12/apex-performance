using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities.Catalogs;

public class AppointmentStatus : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
}