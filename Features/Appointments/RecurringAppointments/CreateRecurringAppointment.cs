using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public record CreateRecurringAppointmentRequest(Guid Client, Guid Coach, Guid TimeSlot);

public record CreateRecurringAppointmentResponse(Guid Id);

public class
    CreateRecurringAppointmentEndpoint : Endpoint<CreateRecurringAppointmentRequest, CreateRecurringAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateRecurringAppointmentEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/recurring-appointments");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator), nameof(UserRoles.Coach));
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CreateRecurringAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var client =
            await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.Client,
                cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var coach =
            await _context.Coaches.FirstOrDefaultAsync(x => x.Id == request.Coach,
                cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var timeSlot =
            await _context.TimeSlots.FirstOrDefaultAsync(x => x.Id == request.TimeSlot,
                cancellationToken: cancellationToken);

        if (timeSlot is null)
            ThrowError(ErrorMessages.NotFound);

        if (!CheckIfRecurringAvailable(request.Coach, request.TimeSlot))
            ThrowError(ValidationMessages.NotValid);

        var newRecurringAppointment = new RecurringAppointment
        {
            Client = client,
            Coach = coach,
            TimeSlot = timeSlot
        };

        _context.RecurringAppointments.Add(newRecurringAppointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new CreateRecurringAppointmentResponse(newRecurringAppointment.Id),
            cancellation: cancellationToken);
    }

    private bool CheckIfRecurringAvailable(Guid coachId, Guid timeSlotId) =>
        !_context.RecurringAppointments
            .Any(x => x.TimeSlotId == timeSlotId && x.CoachId == coachId);
}

public sealed class CreateRecurringAppointmentValidator : Validator<CreateRecurringAppointmentRequest>
{
    public CreateRecurringAppointmentValidator()
    {
        RuleFor(x => x.Client).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Coach).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.TimeSlot).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}