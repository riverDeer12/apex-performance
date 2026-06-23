using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public class CancelAppointmentEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IClientService _clientService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public CancelAppointmentEndpoint(ApexPerformanceContext context, IClientService clientService,
        IEmailService emailService, INotificationService notificationService)
    {
        _context = context;
        _clientService = clientService;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public override void Configure()
    {
        Get("api/appointments/cancel/{id}");
        Permissions(UserPermissions.CanCancelAppointment);
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment =
            await _context.Appointments
                .Include(appointment => appointment.Clients)
                .ThenInclude(clientAppointment => clientAppointment.Client).Include(appointment => appointment.TimeSlot)
                .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorCodes.NotFound);

        var appointmentStatus = await _context.AppointmentStatuses
            .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.Canceled),
                cancellationToken: cancellationToken);

        if (appointmentStatus is null)
            ThrowError(ErrorCodes.NotFound);

        appointment.AppointmentStatus = appointmentStatus;

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        var appointmentClients = appointment.Clients.Select(x => x.Client).ToList();

        await _clientService.AddClientsCredits(appointmentClients, 1, cancellationToken);

        var appointmentClientsIds = appointmentClients.Select(x => x.Id).ToList();
        
        BackgroundJob.Enqueue(() =>
            SendAppointmentStatusFcmNotification(appointmentClientsIds, appointment.Id));

        BackgroundJob.Enqueue(() =>
            SendAppointmentStatusEmail(appointmentClientsIds, appointment.Id));

        await SendAsync(
            new StatusResponse(appointment.Id, true),
            cancellation: cancellationToken);
    }
    
    [AutomaticRetry(Attempts = 0)]
    public async Task SendAppointmentStatusFcmNotification(List<Guid> appointmentClientsIds, Guid appointmentId)
    {
        var appointmentClients = _context.Clients.Where(x => appointmentClientsIds.Contains(x.Id)).ToList();

        var appointment = await _context.Appointments
            .Include(appointment => appointment.TimeSlot)
            .Include(appointment => appointment.AppointmentStatus)
            .SingleAsync(x => x.Id == appointmentId);

        var clientUserIds = appointmentClients.Select(x => x.UserId).ToList();

        var clientDeviceTokens = await _context.DeviceTokens
            .Where(x => clientUserIds.Contains(x.UserId))
            .Select(t => t.Token)
            .ToListAsync();
        
        _ = await _notificationService.SendToMultipleDevices(clientDeviceTokens,
            "You have appointment update",
            "Your Appointment has been " + appointment.AppointmentStatus.Name);
    }

    public async Task SendAppointmentStatusEmail(List<Guid> appointmentClientsIds, Guid appointmentId)
    {
        var appointmentClients = _context.Clients.Where(x => appointmentClientsIds.Contains(x.Id)).ToList();

        var appointment = await _context.Appointments
            .Include(appointment => appointment.TimeSlot)
            .Include(appointment => appointment.AppointmentStatus)
            .SingleAsync(x => x.Id == appointmentId);

        _emailService.SendAppointmentStatus(appointmentClients, appointment, appointment.TimeSlot);
    }
}