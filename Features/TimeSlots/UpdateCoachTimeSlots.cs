using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
    using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public record CreateCoachTimeSlotsRequest(List<Guid> TimeSlots);

public record CreateCoachTimeSlotsResponse(Guid? Id);

public class CreateCoachTimeSlotsEndpoint : Endpoint<CreateCoachTimeSlotsRequest, CreateCoachTimeSlotsResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ITimeSlotService _timeSlotService;

    public CreateCoachTimeSlotsEndpoint(ApexPerformanceContext context, ITimeSlotService timeSlotService)
    {
        _context = context;
        _timeSlotService = timeSlotService;
    }

    public override void Configure()
    {
        Post("api/time-slots/coach/{id}");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(CreateCoachTimeSlotsRequest request, CancellationToken cancellationToken)
    {
        var coachId = Route<Guid>("id", isRequired: true);

        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.Id == coachId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var timeSlots = await _context.TimeSlots
            .Where(x => request.TimeSlots.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        if (timeSlots.Count == 0)
        {
            await SendAsync(new CreateCoachTimeSlotsResponse(null), cancellation: cancellationToken);
            return;
        }

        await _timeSlotService.UpdateCoachTimeSlots(timeSlots, coach, cancellationToken);

        await SendAsync(new CreateCoachTimeSlotsResponse(coachId), cancellation: cancellationToken);
    }
}