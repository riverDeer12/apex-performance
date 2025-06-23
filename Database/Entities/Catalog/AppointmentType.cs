using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class AppointmentType : BaseEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = null!;
}