using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities.Catalog;

public class AppointmentStatus : BaseEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = null!;
    
    public static AppointmentStatus Init(string id, string name, string description)
        => new()
        {
            Id = new Guid(id),
            Name = name,
            Description = description,
        };
}