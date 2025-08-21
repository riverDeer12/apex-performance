using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

[UsedImplicitly]
public record CreateAppointmentRequest(
    Guid Type,
    Guid TimeSlot,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<Guid> Clients,
    List<Guid> Coaches
);

public record CreateAppointmentResponse(
    Guid Id
);

public record AppointmentClientDto(
    Guid Id,
    string FirstName,
    string LastName
);

public class CreateAppointmentEndpoint : Endpoint<CreateAppointmentRequest, CreateAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IAppointmentService _appointmentService;

    public CreateAppointmentEndpoint(ApexPerformanceContext context, IAppointmentService appointmentService)
    {
        _context = context;
        _appointmentService = appointmentService;
    }

    public override void Configure()
    {
        Post("api/appointments");
        Permissions(UserPermissions.CanCreateAppointment);
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var clients = await _context.Clients
            .Where(x => request.Clients.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        if (clients.Count == 0)
            ThrowError(ErrorMessages.NotFound);

        var coaches = await _context.Coaches
            .Where(x => request.Coaches.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        if (coaches.Count == 0)
            ThrowError(ErrorMessages.NotFound);

        var appointmentType = await _context.AppointmentTypes
            .FirstOrDefaultAsync(x => x.Id == request.Type,
                cancellationToken: cancellationToken);

        if (appointmentType is null)
            ThrowError(ErrorMessages.NotFound);

        if (!await _appointmentService.CheckFreeSlot(request.StartTime, request.EndTime, cancellationToken))
            ThrowError(ValidationMessages.NotValid);

        var pendingStatus =
            await _context.AppointmentStatuses
                .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.Pending),
                    cancellationToken: cancellationToken);

        if (pendingStatus is null)
            ThrowError(ErrorMessages.NotFound);
        
        var timeSlot =
            await _context.TimeSlots
                .FirstOrDefaultAsync(x => x.Id == request.TimeSlot,
                    cancellationToken: cancellationToken);

        if (timeSlot is null)
            ThrowError(ErrorMessages.NotFound);
        
        var appointment = new Appointment
        {
            AppointmentType = appointmentType,
            AppointmentStatus = pendingStatus,
            TimeSlot = timeSlot,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        _context.Appointments.Add(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await _appointmentService.UpdateClients(clients, appointment, cancellationToken);

        await _appointmentService.UpdateCoaches(coaches, appointment, cancellationToken);

        await SendAsync(
            new CreateAppointmentResponse(appointment.Id),
            cancellation: cancellationToken);
    }
}

public sealed class CreateAppointmentValidator : Validator<CreateAppointmentRequest>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Clients).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Coaches).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}