using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public class GetCoachTimeSlotsEndpoint : EndpointWithoutRequest<List<GetTimeSlotResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetCoachTimeSlotsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/time-slots/coach/{id}");
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coachId = Route<Guid>("id", isRequired: true);

        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.Id == coachId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var timeSlots = await _context.CoachTimeSlots
            .Where(x => x.CoachId == coachId)
            .Select(x => x.TimeSlotId)
            .ToListAsync(cancellationToken: cancellationToken);

        if (timeSlots.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var coachTimeSlots = _context.TimeSlots.Where(x => timeSlots.Contains(x.Id)).ToList();

        await SendAsync(coachTimeSlots.Select(x
            => new GetTimeSlotResponse(x.Id, x.Day, x.StartTime, x.EndTime))
            .ToList(), cancellation: cancellationToken);
    }
}