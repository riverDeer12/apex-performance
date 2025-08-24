using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class RecurringAppointment : BaseEntity
{
    public Guid ClientId { get; set; }
    public required Client Client { get; set; }
    public Guid CoachId { get; set; }
    public required Coach Coach { get; set; }
    public Guid TimeSlotId { get; set; }
    public required TimeSlot TimeSlot { get; set; }
    
    public bool IsActive { get; set; }
}