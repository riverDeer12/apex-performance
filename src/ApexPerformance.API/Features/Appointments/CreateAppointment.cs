using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using FluentValidation;
using Hangfire;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

[UsedImplicitly]
public record CreateAppointmentRequest(
    Guid Type,
    Guid TimeSlot,
    Guid Location,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<Guid> Clients,
    List<Guid> Coaches
);

public record AppointmentClientDto(
    Guid Id,
    string FirstName,
    string LastName
);

public class CreateAppointmentEndpoint : Endpoint<CreateAppointmentRequest, StatusResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IAppointmentService _appointmentService;
    private readonly IEmailService _emailService;
    private readonly IClientService _clientService;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;

    public CreateAppointmentEndpoint(ApexPerformanceContext context, IAppointmentService appointmentService,
        IEmailService emailService, ICurrentUserService currentUserService, IClientService clientService,
        INotificationService notificationService)
    {
        _context = context;
        _appointmentService = appointmentService;
        _emailService = emailService;
        _currentUserService = currentUserService;
        _clientService = clientService;
        _notificationService = notificationService;
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

        if (!_appointmentService.CheckClientsCredits(clients, cancellationToken))
            ThrowError("Client does not have any credits available.");

        var coaches = await _context.Coaches
            .Where(x => request.Coaches.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        var appointmentType = await _context.AppointmentTypes
            .SingleAsync(x => x.Id == request.Type,
                cancellationToken: cancellationToken);

        var timeSlot =
            await _context.TimeSlots
                .SingleAsync(x => x.Id == request.TimeSlot,
                    cancellationToken: cancellationToken);

        if (!await CheckValidity(request.StartTime, timeSlot, cancellationToken))
        {
            var existingAppointment = await UpdateExistingAppointment(request, cancellationToken, timeSlot, clients);

            await SendAsync(
                new StatusResponse(existingAppointment.Id, true),
                cancellation: cancellationToken);
            return;
        }

        var appointment =
            await CreateNewAppointment(request, cancellationToken, appointmentType, timeSlot, clients, coaches);

        BackgroundJob.Enqueue(() =>
            SendFcmNotifications(request.Coaches, request.Clients, appointment.Id, timeSlot.Id));

        await SendAsync(
            new StatusResponse(appointment.Id, true),
            cancellation: cancellationToken);
    }

    private async Task<Appointment> CreateNewAppointment(CreateAppointmentRequest request,
        CancellationToken cancellationToken,
        AppointmentType appointmentType, TimeSlot timeSlot, List<Client> clients, List<Coach> coaches)
    {
        var appointment = new Appointment
        {
            AppointmentType = appointmentType,
            AppointmentStatus = await GetAppointmentStatus(cancellationToken),
            TimeSlot = timeSlot,
            StartTime = request.StartTime.UtcDateTime,
            EndTime = request.EndTime.UtcDateTime
        };

        _context.Appointments.Add(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await _appointmentService.UpdateClients(clients, appointment, cancellationToken);

        await _appointmentService.UpdateCoaches(coaches, appointment, cancellationToken);

        if (appointment.AppointmentStatus.Name == BusinessStatuses.Approved)
            await _clientService.RemoveClientsCredits(clients, 1, cancellationToken);
        return appointment;
    }

    private async Task<Appointment> UpdateExistingAppointment(CreateAppointmentRequest request,
        CancellationToken cancellationToken,
        TimeSlot timeSlot, List<Client> requestClients)
    {
        var existingAppointment = await _context.Appointments
            .Include(x => x.Clients)
            .FirstOrDefaultAsync(x =>
                    x.TimeSlotId == timeSlot.Id &&
                    x.StartTime.Date == request.StartTime.Date,
                cancellationToken: cancellationToken);

        if (existingAppointment is null)
            ThrowError(ErrorCodes.NotFound);

        foreach (var client in requestClients)
        {
            var clientAppointment = new ClientAppointment
            {
                ClientId = client.Id,
                Client = client,
                AppointmentId = existingAppointment.Id,
                Appointment = existingAppointment
            };

            existingAppointment.Clients.Add(clientAppointment);
        }
        
        BackgroundJob.Enqueue(() =>
            SendFcmNotifications(request.Coaches, request.Clients, existingAppointment.Id, timeSlot.Id));
        
        BackgroundJob.Enqueue(() =>
            SendEmailNotifications(request.Coaches, request.Clients, existingAppointment.Id, timeSlot.Id));

        _context.Appointments.Update(existingAppointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        var removeCreditClients = new List<Client>(requestClients);

        await _clientService.RemoveClientsCredits(removeCreditClients, 1, cancellationToken);
        
        return existingAppointment;
    }

    public async Task SendEmailNotifications(List<Guid> coachesIds, List<Guid> clientsIds, Guid appointmentId,
        Guid timeSlotId)
    {
        var appointment =
            await _context.Appointments
                .Include(appointment => appointment.AppointmentStatus)
                .SingleAsync(x => x.Id == appointmentId);

        var clients = await _context.Clients
            .Where(x => clientsIds.Contains(x.Id))
            .ToListAsync();

        var coaches = await _context.Coaches
            .Where(x => coachesIds.Contains(x.Id))
            .ToListAsync();
        
        var timeSlot =
            await _context.TimeSlots
                .SingleAsync(x => x.Id == timeSlotId);
        
        _emailService.SendAppointmentRequestEmail(coaches, clients, appointment, timeSlot);

        _emailService.SendAppointmentStatus(clients, appointment, timeSlot);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task SendFcmNotifications(List<Guid> coachesIds, List<Guid> clientsIds, Guid appointmentId,
        Guid timeSlotId)
    {
        var appointment =
            await _context.Appointments
                .Include(appointment => appointment.AppointmentStatus)
                .SingleAsync(x => x.Id == appointmentId);

        var clients = await _context.Clients
            .Where(x => clientsIds.Contains(x.Id))
            .ToListAsync();

        var coaches = await _context.Coaches
            .Where(x => coachesIds.Contains(x.Id))
            .ToListAsync();

        var coachUserIds = coaches.Select(x => x.UserId).ToList();

        var clientUserIds = clients.Select(x => x.UserId).ToList();

        var clientDeviceTokens = await _context.DeviceTokens
            .Where(x => clientUserIds.Contains(x.UserId))
            .Select(t => t.Token)
            .ToListAsync();

        var coachDeviceTokens = await _context.DeviceTokens
            .Where(x => coachUserIds.Contains(x.UserId))
            .Select(t => t.Token)
            .ToListAsync();

        _ = await _notificationService.SendToMultipleDevices(coachDeviceTokens, "Appointment Request",
            "New Appointment Requested.");

        _ = await _notificationService.SendToMultipleDevices(clientDeviceTokens,
            "You have appointment update",
            "Your Appointment has been " + appointment.AppointmentStatus.Name);
    }
    
    private async Task<bool> CheckValidity(DateTimeOffset requestStartTime, TimeSlot timeSlot,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;

        var requestedDay = requestStartTime.Date;

        if (today == requestedDay && _currentUserService.LoggedUserHasRole(UserRoles.Client))
            return false;

        if (!await _appointmentService.CheckFreeSlot(requestStartTime, timeSlot, cancellationToken))
            return false;

        return true;
    }

    private async Task<AppointmentStatus> GetAppointmentStatus(CancellationToken cancellationToken)
    {
        var pendingStatus =
            await _context.AppointmentStatuses
                .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.Pending),
                    cancellationToken: cancellationToken);

        if (pendingStatus is null)
            ThrowError("Pending Status not found.");

        var approvedStatus =
            await _context.AppointmentStatuses
                .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.Approved),
                    cancellationToken: cancellationToken);

        if (approvedStatus is null)
            ThrowError("Approved Status not found.");

        return _currentUserService.LoggedUserHasRole(UserRoles.Client) ? pendingStatus : approvedStatus;
    }
}

public sealed class CreateAppointmentValidator : Validator<CreateAppointmentRequest>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();

                return db.AppointmentTypes.AnyAsync(appointmentType => appointmentType.Id == id, cancellationToken);
            })
            .WithMessage(ErrorCodes.NotFound);

        RuleFor(x => x.TimeSlot)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();

                return db.TimeSlots.AnyAsync(appointmentType => appointmentType.Id == id, cancellationToken);
            })
            .WithMessage(ErrorCodes.NotFound);

        RuleFor(x => x.Clients)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage(ErrorCodes.DuplicatesNotAllowed)
            .MustAsync(async (clientIds, cancellationToken) =>
            {
                var db = Resolve<ApexPerformanceContext>();
                var numberOfClients = await db.Clients
                    .Where(client => clientIds.Contains(client.Id))
                    .CountAsync(cancellationToken);

                return numberOfClients == clientIds.Count;
            })
            .WithMessage(ErrorCodes.NotFound);

        RuleFor(x => x.Coaches)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage(ErrorCodes.DuplicatesNotAllowed)
            .MustAsync(async (coachIds, cancellationToken) =>
            {
                var db = Resolve<ApexPerformanceContext>();
                var numberOfCoaches = await db.Coaches
                    .Where(coach => coachIds.Contains(coach.Id))
                    .CountAsync(cancellationToken);

                return numberOfCoaches == coachIds.Count;
            })
            .WithMessage(ErrorCodes.NotFound);
    }
}