using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class RecurringAppointment : BaseEntity
{
    public Guid CoachId { get; set; }
    public required Coach Coach { get; set; }
    public Guid TimeSlotId { get; set; }
    public required TimeSlot TimeSlot { get; set; }
    public required AppointmentType AppointmentType { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public bool IsActive { get; set; }
    public ICollection<ClientRecurringAppointment> Clients  { get; set; } = null!;
}