using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public record UpdateCoachTimeSlotsRequest(List<Guid> TimeSlots, Guid Coach);

public record UpdateCoachTimeSlotsResponse(Guid? Id);

public class UpdateCoachTimeSlotsEndpoint : Endpoint<UpdateCoachTimeSlotsRequest, UpdateCoachTimeSlotsResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ITimeSlotService _timeSlotService;

    public UpdateCoachTimeSlotsEndpoint(ApexPerformanceContext context, ITimeSlotService timeSlotService)
    {
        _context = context;
        _timeSlotService = timeSlotService;
    }

    public override void Configure()
    {
        Post("api/time-slots/coach");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(UpdateCoachTimeSlotsRequest request, CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.Id == request.Coach,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorCodes.NotFound);

        var timeSlots = await _context.TimeSlots
            .Where(x => request.TimeSlots.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        if (timeSlots.Count == 0)
        {
            await SendAsync(new UpdateCoachTimeSlotsResponse(null), cancellation: cancellationToken);
            return;
        }

        await _timeSlotService.UpdateCoachTimeSlots(timeSlots, coach, cancellationToken);

        await SendAsync(new UpdateCoachTimeSlotsResponse(coach.Id), cancellation: cancellationToken);
    }
}

public sealed class UpdateCoachTimeSlotsRequestValidator : Validator<UpdateCoachTimeSlotsRequest>
{
    public UpdateCoachTimeSlotsRequestValidator()
    {
        RuleFor(x => x.Coach)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();

                return db.Coaches.AnyAsync(coach => coach.Id == id, cancellationToken);
            })
            .WithMessage(ErrorCodes.NotFound);

        RuleFor(x => x.TimeSlots).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}