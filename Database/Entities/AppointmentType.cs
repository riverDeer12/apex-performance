namespace ApexPerformance.API.Database.Entities;

public class AppointmentType : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ICollection<Appointment> Appointments { get; set; }
}