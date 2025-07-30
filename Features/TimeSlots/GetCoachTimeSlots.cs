using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public record GetCoachTimeSlotRequest(List<Guid> Coaches, DateTimeOffset Day);

public class GetCoachTimeSlotsEndpoint : Endpoint<GetCoachTimeSlotRequest, List<GetTimeSlotResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetCoachTimeSlotsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/time-slots/coach");
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(GetCoachTimeSlotRequest request, CancellationToken cancellationToken)
    {
        var coaches = await _context.Coaches
            .Where(x => request.Coaches.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (coaches.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var timeSlots = await _context.CoachTimeSlots
            .Where(x => coaches.Contains(x.CoachId))
            .Select(x => x.TimeSlotId)
            .ToListAsync(cancellationToken: cancellationToken);

        if (timeSlots.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var coachesTimeSlots = _context.TimeSlots.Where(x => timeSlots.Contains(x.Id)).ToList();

        await SendAsync(coachesTimeSlots.Select(x
                => new GetTimeSlotResponse(x.Id, $"{x.StartTime} - {x.EndTime}", x.StartTime, x.EndTime))
            .ToList(), cancellation: cancellationToken);
    }
}