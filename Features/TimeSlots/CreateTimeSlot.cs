using System.ComponentModel;
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

        var newTimeSlot = new TimeSlot
        {
            Day = request.Day,
            StartTime = TimeOnly.Parse(request.StartTime),
            EndTime = TimeOnly.Parse(request.EndTime) 
        };

        _context.TimeSlots.Add(newTimeSlot);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception(ErrorMessages.SavingError);

        var coachTimeSlot = new CoachTimeSlot
        {
            CoachId = coach.Id,
            Coach = coach,
            TimeSlotId = newTimeSlot.Id,
            TimeSlot = newTimeSlot
        };

        _context.CoachTimeSlots.Add(coachTimeSlot);

        var coachTimeSlotResult = await _context.SaveChangesAsync(cancellationToken);

        if (coachTimeSlotResult == 0)
            throw new Exception(ErrorMessages.SavingError);

        await SendAsync(new CreateTimeSlotResponse(newTimeSlot.Id), cancellation: cancellationToken);
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