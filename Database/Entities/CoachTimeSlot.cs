using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class CoachTimeSlot
{
    public Guid CoachId { get; set; }

    public Coach Coach { get; set; } = null!;

    public Guid TimeSlotId { get; set; }

    public TimeSlot TimeSlot { get; set; } = null!;
}