using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public record GetTimeSlotResponse(Guid Id, string Name, TimeOnly StartTime, TimeOnly EndTime);

public class GetAllTimeSlotsEndpoint : EndpointWithoutRequest<List<GetTimeSlotResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAllTimeSlotsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/time-slots");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var timeSlots = (await _context.TimeSlots
            .OrderBy(x => x.StartTime)
            .ToListAsync(cancellationToken))
            .GroupBy(x => x.StartTime)
            .Select(g => g.First())
            .ToList();

        if (timeSlots.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(timeSlots.Select(x
                => new GetTimeSlotResponse(x.Id, $"{x.StartTime} - {x.EndTime}", x.StartTime, x.EndTime))
            .ToList(), cancellation: cancellationToken);
    }
}