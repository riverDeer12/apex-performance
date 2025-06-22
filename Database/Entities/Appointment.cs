using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalogs;

namespace ApexPerformance.API.Database.Entities;

public class Appointment : BaseEntity
{
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public AppointmentType AppointmentType { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public AppointmentStatus Status { get; set; }
    public ICollection<ClientAppointment> Clients { get; set; }
}