using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.Extensions;
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

    public List<TimeSlot> GetCoachTimeSlotsForDay(List<TimeSlot> coachesTimeSlots, DateTime day)
    {
        var requestedDay = day.DayOfWeek;
        
        return coachesTimeSlots.Where(x => x.Day == requestedDay).ToList();
    }
}