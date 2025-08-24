using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public record ChangeCoachTimeSlotActivityResponse(Guid Id);

public class ChangeCoachTimeSlotActivityEndpoint : EndpointWithoutRequest<ChangeCoachTimeSlotActivityResponse>
{
    private readonly ApexPerformanceContext _context;

    public ChangeCoachTimeSlotActivityEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/time-slots/{timeSlotId}/coach/{coachId}/activity");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var timeSlotId = Route<Guid>("timeSlotId", isRequired: true);

        var coachId = Route<Guid>("coachId", isRequired: true);

        var timeSlot =
            await _context.TimeSlots.FirstOrDefaultAsync(x => x.Id == timeSlotId,
                cancellationToken: cancellationToken);

        if (timeSlot is null)
            ThrowError(ErrorMessages.NotFound);

        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.Id == coachId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var coachTimeSlot =
            await _context.CoachTimeSlots.FirstOrDefaultAsync(x => x.TimeSlotId == timeSlot.Id && x.CoachId == coach.Id,
                cancellationToken: cancellationToken);
        
        if(coachTimeSlot is null)
            ThrowError(ErrorMessages.NotFound);

        coachTimeSlot.IsActive = !coachTimeSlot.IsActive;
        
        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception(ErrorMessages.SavingError);

        await SendAsync(new ChangeCoachTimeSlotActivityResponse(timeSlot.Id), cancellation: cancellationToken);
    }
}