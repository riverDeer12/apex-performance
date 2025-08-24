using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public class GetCoachTimeSlotsEndpoint : EndpointWithoutRequest<List<TimeSlotDto>>
{
    private readonly ApexPerformanceContext _context;

    private readonly ICurrentUserService _currentUserService;

    public GetCoachTimeSlotsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/time-slots");
        Roles(UserRoles.Coach);
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var coachTimeSlots = await _context.CoachTimeSlots
            .Where(x => x.CoachId == coach.Id && x.IsActive)
            .Include(coachTimeSlot => coachTimeSlot.TimeSlot)
            .OrderBy(x => x.TimeSlot.Day)
            .ToListAsync(cancellationToken: cancellationToken);

        if (coachTimeSlots.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(
            coachTimeSlots.Select(x => new TimeSlotDto(x.TimeSlot.Id, x.TimeSlot.Name, x.TimeSlot.Day,
                    x.TimeSlot.StartTime, x.TimeSlot.EndTime, x.IsActive))
                .ToList(), cancellation: cancellationToken);
    }
}