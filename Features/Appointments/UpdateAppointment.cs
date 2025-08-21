using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using EFCore.BulkExtensions;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record UpdateAppointmentRequest(
    Guid Type,
    Guid TimeSlot,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<Guid> Clients,
    List<Guid> Coaches
);

public record UpdateAppointmentResponse(
    Guid Id
);

public class UpdateAppointmentEndpoint : Endpoint<UpdateAppointmentRequest, UpdateAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IAppointmentService _appointmentService;
    private readonly IEmailService _emailService;

    public UpdateAppointmentEndpoint(ApexPerformanceContext context, IAppointmentService appointmentService,
        IEmailService emailService)
    {
        _context = context;
        _appointmentService = appointmentService;
        _emailService = emailService;
    }

    public override void Configure()
    {
        Put("api/appointments/{id}");
        Permissions(UserPermissions.CanUpdateAppointment);
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(UpdateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment =
            await _context.Appointments
                .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.NotFound);

        if (!await _appointmentService.CheckFreeSlot(request.StartTime, request.EndTime,
                cancellationToken, appointmentId))
            ThrowError(ValidationMessages.NotValid);

        var appointmentType = await _context.AppointmentTypes.FirstOrDefaultAsync(
            x => x.Id == request.Type, cancellationToken: cancellationToken);

        if (appointmentType is null)
            ThrowError(ErrorMessages.NotFound);

        var inProgressStatus = _context.AppointmentStatuses
            .FirstOrDefault(x => x.Name == nameof(BusinessStatuses.InProgress));

        if (inProgressStatus == null)
            ThrowError(ErrorMessages.NotFound);
        
        var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(
            x => x.Id == request.TimeSlot, cancellationToken: cancellationToken);

        if (timeSlot is null)
            ThrowError(ErrorMessages.NotFound);

        appointment.StartTime = request.StartTime;
        appointment.EndTime = request.EndTime;
        appointment.AppointmentType = appointmentType;
        appointment.AppointmentStatus = inProgressStatus;
        appointment.TimeSlot = timeSlot;

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await _context.ClientAppointments
            .Where(clientAppointment => clientAppointment.AppointmentId == appointment.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.CoachAppointments
            .Where(coachAppointment => coachAppointment.AppointmentId == appointment.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var clients = await _context.Clients
            .Where(x => request.Clients.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        var coaches = await _context.Coaches
            .Where(x => request.Coaches.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        await _appointmentService.UpdateClients(clients, appointment, cancellationToken);

        await _appointmentService.UpdateCoaches(coaches, appointment, cancellationToken);

        _emailService.SendAppointmentStatus(clients, appointment);

        await SendAsync(
            new UpdateAppointmentResponse(appointment.Id), cancellation: cancellationToken);
    }
}

public sealed class UpdateAppointmentValidator : Validator<UpdateAppointmentRequest>
{
    public UpdateAppointmentValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.StartTime).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.EndTime).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Clients).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Coaches).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}