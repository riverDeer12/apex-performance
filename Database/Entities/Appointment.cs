using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class Appointment : BaseEntity
{
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public required AppointmentType AppointmentType { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public required AppointmentStatus AppointmentStatus { get; set; }
    public Guid AppointmentStatusId { get; set; }
    public ICollection<ClientAppointment> Clients { get; set; } = null!;
}