using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Services;

public interface ITimeSlotService
{
    Task UpdateCoachTimeSlots(List<TimeSlot> timeSlots, Coach coach, CancellationToken cancellationToken);

    Task<List<TimeSlot>> CheckTimeSlotsAvailability(List<TimeSlot> coachesTimeSlots, DayOfWeek day,
        CancellationToken cancellationToken);
}