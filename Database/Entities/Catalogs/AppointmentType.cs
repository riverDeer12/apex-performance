using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities.Catalogs;

public class AppointmentType : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ICollection<Appointment> Appointments { get; set; }
}