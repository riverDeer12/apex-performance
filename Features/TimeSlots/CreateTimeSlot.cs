using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public record CreateTimeSlotRequest(Guid Coach, DayOfWeek Day, string StartTime, string EndTime);

public record CreateTimeSlotResponse(Guid Id);

public class CreateTimeSlotEndpoint : Endpoint<CreateTimeSlotRequest, CreateTimeSlotResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateTimeSlotEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/time-slots");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(CreateTimeSlotRequest request, CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.Id == request.Coach,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var startTime = TimeOnly.Parse(request.StartTime);

        var endTime = TimeOnly.Parse(request.EndTime);

        var existingTimeSlot =
            await _context.TimeSlots.FirstOrDefaultAsync(
                x => x.StartTime == startTime && x.EndTime == endTime && x.Day == request.Day,
                cancellationToken: cancellationToken);

        if (existingTimeSlot is not null)
        {
            var coachHasExistingTimeSlot =
                _context.CoachTimeSlots.Any(x => x.TimeSlotId == existingTimeSlot.Id &&
                                                 x.CoachId == coach.Id);
            if (coachHasExistingTimeSlot)
                ThrowError(ValidationMessages.NotValid);

            await CreateCoachTimeSlot(coach, existingTimeSlot, cancellationToken);

            await SendAsync(new CreateTimeSlotResponse(existingTimeSlot.Id), cancellation: cancellationToken);
        }
        else
        {
            var newTimeSlot = new TimeSlot
            {
                Day = request.Day,
                StartTime = startTime,
                EndTime = endTime
            };

            _context.TimeSlots.Add(newTimeSlot);

            var result = await _context.SaveChangesAsync(cancellationToken);

            if (result == 0)
                throw new Exception(ErrorMessages.SavingError);

            await CreateCoachTimeSlot(coach, newTimeSlot, cancellationToken);

            await SendAsync(new CreateTimeSlotResponse(newTimeSlot.Id), cancellation: cancellationToken);
        }
    }

    private async Task CreateCoachTimeSlot(Coach coach, TimeSlot timeSlot, CancellationToken cancellationToken)
    {
        var coachTimeSlot = new CoachTimeSlot
        {
            CoachId = coach.Id,
            Coach = coach,
            TimeSlotId = timeSlot.Id,
            TimeSlot = timeSlot
        };

        _context.CoachTimeSlots.Add(coachTimeSlot);

        var coachTimeSlotResult = await _context.SaveChangesAsync(cancellationToken);

        if (coachTimeSlotResult == 0)
            throw new Exception(ErrorMessages.SavingError);
    }
}

public sealed class CreateTimeSlotValidator : Validator<CreateTimeSlotRequest>
{
    public CreateTimeSlotValidator()
    {
        RuleFor(x => x.Coach).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Day).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.StartTime).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.EndTime).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}