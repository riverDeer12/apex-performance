using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public record CreateRecurringAppointmentRequest(List<Guid> Clients, Guid Coach, Guid TimeSlot, Guid Type);

public record CreateRecurringAppointmentResponse(Guid Id);

public class
    CreateRecurringAppointmentEndpoint : Endpoint<CreateRecurringAppointmentRequest, CreateRecurringAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IRecurringAppointmentService _recurringAppointmentService;

    public CreateRecurringAppointmentEndpoint(ApexPerformanceContext context,
        IRecurringAppointmentService recurringAppointmentService)
    {
        _context = context;
        _recurringAppointmentService = recurringAppointmentService;
    }

    public override void Configure()
    {
        Post("api/recurring-appointments");
        Permissions(UserPermissions.CanCreateRecurringAppointment);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CreateRecurringAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var coach =
            await _context.Coaches.SingleAsync(x => x.Id == request.Coach,
                cancellationToken: cancellationToken);

        var timeSlot =
            await _context.TimeSlots.SingleAsync(x => x.Id == request.TimeSlot,
                cancellationToken: cancellationToken);

        var type =
            await _context.AppointmentTypes.SingleAsync(x => x.Id == request.Type,
                cancellationToken: cancellationToken);

        var clients = _context.Clients
            .Where(x => request.Clients.Contains(x.Id))
            .ToList();

        if (!_recurringAppointmentService.CheckIfRecurringAvailable(request.Coach, request.TimeSlot))
            ThrowError(ValidationMessages.NotValid);

        var newRecurringAppointment = new RecurringAppointment
        {
            Coach = coach,
            TimeSlot = timeSlot,
            AppointmentType = type,
            IsActive = true
        };

        _context.RecurringAppointments.Add(newRecurringAppointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await _recurringAppointmentService.UpdateClients(clients, newRecurringAppointment, cancellationToken);

        await SendAsync(new CreateRecurringAppointmentResponse(newRecurringAppointment.Id),
            cancellation: cancellationToken);
    }
}

public sealed class CreateRecurringAppointmentValidator
    : Validator<CreateRecurringAppointmentRequest>
{
    public CreateRecurringAppointmentValidator()
    {
        RuleFor(x => x.Clients)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage(ValidationMessages.DuplicatesNotAllowed)
            .MustAsync(async (clientIds, cancellationToken) =>
            {
                var db = Resolve<ApexPerformanceContext>();
                var numberOfClients = await db.Clients
                    .Where(client => clientIds.Contains(client.Id))
                    .CountAsync(cancellationToken);

                return numberOfClients == clientIds.Count;
            })
            .WithMessage(ErrorMessages.NotFound);

        RuleFor(x => x.Coach)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();
                return db.Coaches.AnyAsync(coach => coach.Id == id, cancellationToken);
            })
            .WithMessage(ErrorMessages.NotFound);

        RuleFor(x => x.TimeSlot)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();
                return db.TimeSlots.AnyAsync(timeSlot => timeSlot.Id == id, cancellationToken);
            })
            .WithMessage(ErrorMessages.NotFound);

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();
                return db.AppointmentTypes.AnyAsync(appointmentType => appointmentType.Id == id,
                    cancellationToken);
            })
            .WithMessage(ErrorMessages.NotFound);
    }
}