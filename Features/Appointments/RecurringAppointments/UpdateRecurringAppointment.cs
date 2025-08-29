using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public record UpdateRecurringAppointmentRequest(List<Guid> Clients, Guid Coach, Guid TimeSlot, Guid Type);

public record UpdateRecurringAppointmentResponse(Guid Id);

public class
    UpdateRecurringAppointmentEndpoint : Endpoint<UpdateRecurringAppointmentRequest, UpdateRecurringAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateRecurringAppointmentEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/recurring-appointments/{id}");
        Permissions(UserPermissions.CanUpdateRecurringAppointment);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(UpdateRecurringAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var recurringAppointmentId = Route<Guid>("id", isRequired: true);

        var recurringAppointment =
            await _context.RecurringAppointments
                .FirstOrDefaultAsync(x => x.Id == recurringAppointmentId,
                    cancellationToken: cancellationToken);

        if (recurringAppointment is null)
            ThrowError(ErrorMessages.NotFound);

        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.Id == request.Coach,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);
        
        var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(x => x.Id == request.TimeSlot,
            cancellationToken: cancellationToken);

        if (timeSlot is null)
            ThrowError(ErrorMessages.NotFound);
        
        var type = await _context.AppointmentTypes.FirstOrDefaultAsync(x => x.Id == request.Type,
            cancellationToken: cancellationToken);

        if (type is null)
            ThrowError(ErrorMessages.NotFound);
        
        
    }
}

public sealed class UpdateRecurringAppointmentValidator : Validator<UpdateRecurringAppointmentRequest>
{
    public UpdateRecurringAppointmentValidator()
    {
        RuleFor(x => x.Clients).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Coach).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.TimeSlot).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Type).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}