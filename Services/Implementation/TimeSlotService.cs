using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services.Implementation;

public class TimeSlotService : ITimeSlotService
{
    private readonly ApexPerformanceContext _context;

    public TimeSlotService(ApexPerformanceContext context)
    {
        _context = context;
    }

    public async Task UpdateCoachTimeSlots(List<TimeSlot> timeSlots,
        Coach coach, CancellationToken cancellationToken)
    {
        await _context.CoachTimeSlots
            .Where(coachTimeSlot => coachTimeSlot.CoachId == coach.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var coachTimeSlots = timeSlots
            .Select(timeSlot => new CoachTimeSlot
            {
                TimeSlot = timeSlot,
                TimeSlotId = timeSlot.Id,
                Coach = coach,
                CoachId = coach.Id
            }).ToList();

        await _context.BulkInsertOrUpdateAsync(coachTimeSlots, cancellationToken: cancellationToken);
    }

    public async Task<List<TimeSlot>> CheckTimeSlotsAvailability(List<TimeSlot> coachesTimeSlots, DayOfWeek day,
        CancellationToken cancellationToken)
    {
        var timeSlotsIds = coachesTimeSlots
            .Select(x => x.Id)
            .ToList();

        var takenTimeSlots = await _context.Appointments
            .Where(x => timeSlotsIds.Contains(x.TimeSlotId))
            .Select(x => x.TimeSlot)
            .ToListAsync(cancellationToken: cancellationToken);

        if (takenTimeSlots.Count == 0) return coachesTimeSlots;
        
        var availableTimeSlotIds = coachesTimeSlots
            .Where(timeSlot => !takenTimeSlots.Contains(timeSlot))
            .ToList();

        return availableTimeSlotIds;
    }
}