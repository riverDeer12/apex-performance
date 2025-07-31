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
    
    public required TimeSlot TimeSlot { get; set; }
    
    public Guid TimeSlotId { get; set; }
    public ICollection<ClientAppointment> Clients { get; set; } = null!;
    public ICollection<CoachAppointment> Coaches { get; set; } = null!;
}