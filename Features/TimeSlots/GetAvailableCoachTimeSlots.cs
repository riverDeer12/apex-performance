using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public record GetAvailableCoachTimeSlotRequest(List<Guid> Coaches, int Day);

public class GetAvailableCoachTimeSlotsEndpoint : Endpoint<GetAvailableCoachTimeSlotRequest, List<GetTimeSlotResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ITimeSlotService _timeSlotService;

    public GetAvailableCoachTimeSlotsEndpoint(ApexPerformanceContext context, ITimeSlotService timeSlotService)
    {
        _context = context;
        _timeSlotService = timeSlotService;
    }

    public override void Configure()
    {
        Post("api/time-slots/available");
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(GetAvailableCoachTimeSlotRequest request,
        CancellationToken cancellationToken)
    {
        var coachesIds = await _context.Coaches
            .Where(x => request.Coaches.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (coachesIds.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var timeSlots = await _context.CoachTimeSlots
            .Where(x => coachesIds.Contains(x.CoachId) && x.IsActive)
            .Select(x => x.TimeSlot)
            .ToListAsync(cancellationToken: cancellationToken);

        if (timeSlots.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var day = (DayOfWeek)request.Day;
        
        var coachesTimeSlots = timeSlots
            .Where(x => x.Day == day)
            .ToList();

        var finalTimeSlots = await _timeSlotService.CheckTimeSlotsAvailability(coachesTimeSlots, day,
            cancellationToken);
        
        await SendAsync(finalTimeSlots.Select(x
                => new GetTimeSlotResponse(x.Id, $"{x.StartTime} - {x.EndTime}",
                    Enum.GetName(typeof(DayOfWeek), x.Day)!,
                    x.StartTime, x.EndTime))
            .OrderBy(x => x.StartTime)
            .ToList(), cancellation: cancellationToken);
    }
}