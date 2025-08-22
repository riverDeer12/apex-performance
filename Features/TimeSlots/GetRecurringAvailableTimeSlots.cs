using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public class GetRecurringAvailableTimeSlotsEndpoint : EndpointWithoutRequest<List<GetTimeSlotResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetRecurringAvailableTimeSlotsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/recurring-appointments/available/{id}");
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coachId = Route<Guid>("id", isRequired: true);

        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.Id == coachId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var coachTimeSlots = await _context.CoachTimeSlots
            .Where(x => x.CoachId == coach.Id)
            .Include(coachTimeSlot => coachTimeSlot.TimeSlot)
            .ToListAsync(cancellationToken: cancellationToken);

        if (coachTimeSlots.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var takenTimeSlotIds = await _context.RecurringAppointments.Where(x => x.CoachId == coach.Id)
            .Select(x => x.TimeSlotId)
            .ToListAsync(cancellationToken: cancellationToken);

        var availableTimeSlots = coachTimeSlots
            .Where(x => !takenTimeSlotIds.Contains(x.TimeSlotId))
            .ToList();

        await SendAsync(availableTimeSlots.Select(x =>
            new GetTimeSlotResponse(x.TimeSlot.Id, x.TimeSlot.Name,
                Enum.GetName(typeof(DayOfWeek), x.TimeSlot.Day)!,
                x.TimeSlot.StartTime,
                x.TimeSlot.EndTime)).ToList(), cancellation: cancellationToken);
    }
}