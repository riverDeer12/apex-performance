using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.Extensions;
using FastEndpoints;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public class GenerateNextWeekAppointmentsEndpoint : EndpointWithoutRequest<int>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppointmentService _appointmentService;
    private readonly IClientService _clientService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public GenerateNextWeekAppointmentsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IAppointmentService appointmentService, IEmailService emailService, IClientService clientService,
        INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _appointmentService = appointmentService;
        _emailService = emailService;
        _clientService = clientService;
        _notificationService = notificationService;
    }

    public override void Configure()
    {
        Get("api/recurring-appointments/generate-next-week");
        Roles(UserRoles.Coach);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coach =
            await _context.Coaches
                .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
                    cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorCodes.NotFound);

        var coachRecurringAppointments =
            await _context.RecurringAppointments
                .Where(x => x.CoachId == coach.Id)
                .Include(recurringAppointment => recurringAppointment.TimeSlot)
                .Include(recurringAppointment => recurringAppointment.AppointmentType)
                .Include(recurringAppointment => recurringAppointment.Clients)
                .ThenInclude(clientRecurringAppointment => clientRecurringAppointment.Client)
                .Include(recurringAppointment => recurringAppointment.Coach)
                .ToListAsync(cancellationToken: cancellationToken);

        if (coachRecurringAppointments.Count is 0)
        {
            await SendAsync(StatusCodes.Status204NoContent, cancellation: cancellationToken);
            return;
        }

        var approvedStatus =
            await _context.AppointmentStatuses.FirstOrDefaultAsync(x => x.Name == BusinessStatuses.Approved,
                cancellationToken: cancellationToken);

        if (approvedStatus is null)
            ThrowError(ErrorCodes.NotFound);

        var recurringClients = coachRecurringAppointments
            .SelectMany(x => x.Clients.Select(y => y.Client))
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();

        var nextWeekAppointments = new List<Appointment>();

        var removeCreditClients = new List<Client>();

        foreach (var recurring in coachRecurringAppointments)
        {
            var newAppointment = await CreateAppointment(recurring, approvedStatus, cancellationToken);

            if (newAppointment is null) continue;

            nextWeekAppointments.Add(newAppointment);

            removeCreditClients.AddRange(recurring.Clients.Select(x => x.Client).ToList());
        }

        if (nextWeekAppointments.Count is 0)
        {
            await SendAsync(StatusCodes.Status204NoContent, cancellation: cancellationToken);
            return;
        }

        _context.Appointments.AddRange(nextWeekAppointments);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await _clientService.RemoveClientsCredits(removeCreditClients, 1, cancellationToken);

        var emailClients = recurringClients.DistinctBy(x => x.Id).ToList();

        var nextWeekAppointmentsIds = nextWeekAppointments.Select(x => x.Id).ToList();
        
        BackgroundJob.Enqueue(() =>
            SendNextWeekFcmNotifications(emailClients.Select(x => x.Id).ToList(), nextWeekAppointmentsIds));

        BackgroundJob.Enqueue(() =>
            SendNextWeekNotificationEmails(emailClients.Select(x => x.Id).ToList(), nextWeekAppointmentsIds));

        await SendAsync(StatusCodes.Status201Created, cancellation: cancellationToken);
    }
    
    [AutomaticRetry(Attempts = 0)]
    public async Task SendNextWeekFcmNotifications(List<Guid> clientIds,
        List<Guid> nextWeekAppointmentsIds)
    {
        var nextWeekAppointments = await _context.Appointments
            .Where(appointment => nextWeekAppointmentsIds.Contains(appointment.Id))
            .Include(appointment => appointment.TimeSlot)
            .Include(appointment => appointment.Coaches)
            .ThenInclude(appointment => appointment.Coach)
            .Include(appointment => appointment.AppointmentType).Include(appointment => appointment.Clients)
            .ToListAsync();

        var clients = await _context.Clients.Where(x => clientIds.Contains(x.Id)).ToListAsync();

        var clientUserIds = clients.Select(x => x.UserId).ToList();

        var deviceTokens = await _context.DeviceTokens
            .Where(x => clientUserIds.Contains(x.UserId))
            .ToListAsync();

        foreach (var client in clients)
        {
            var clientAppointments = nextWeekAppointments
                .Where(appointment => appointment.Clients.Any(c => c.ClientId == client.Id))
                .ToList();

            if (clientAppointments.Count is 0) continue;

            var clientDeviceTokens = deviceTokens
                .Where(t => t.UserId == client.UserId)
                .Select(t => t.Token)
                .ToList();

            if (clientDeviceTokens.Count is 0) continue;

            var notificationBody = PrepareNotificationBody(clientAppointments);

            _ = await _notificationService.SendToMultipleDevices(clientDeviceTokens,
                "Your weekly training schedule is ready",
                notificationBody,
                PushNotificationTypes.Data(PushNotificationTypes.AppointmentUpdated));
        }
    }

    public void SendNextWeekNotificationEmails(List<Guid> emailClientIds,
        List<Guid> nextWeekAppointmentsIds)
    {
        var nextWeekAppointments = _context.Appointments
            .Where(appointment => nextWeekAppointmentsIds.Contains(appointment.Id))
            .Include(appointment => appointment.TimeSlot)
            .Include(appointment => appointment.Coaches)
            .ThenInclude(appointment => appointment.Coach)
            .Include(appointment => appointment.AppointmentType).Include(appointment => appointment.Clients)
            .ToList();

        var emailClients = _context.Clients.Where(x => emailClientIds.Contains(x.Id)).ToList();

        foreach (var emailClient in emailClients)
        {
            var emailClientAppointments = nextWeekAppointments
                .Where(appointment => appointment.Clients.Any(c => c.ClientId == emailClient.Id))
                .ToList();

            var emailBody = PrepareEmailBody(emailClientAppointments);

            if (!string.IsNullOrEmpty(emailBody))
                _emailService.SendWeekAppointmentsSchedule(emailClient, emailBody);
        }
    }

    private string PrepareNotificationBody(List<Appointment> clientAppointments)
    {
        var lines = new List<string>();

        foreach (var appointment in clientAppointments)
        {
            var day = Enum.GetName(typeof(DayOfWeek), appointment.TimeSlot.Day);

            var appointmentTime = $"{appointment.TimeSlot.StartTime} - {appointment.TimeSlot.EndTime}";

            var coach = appointment.Coaches.First().Coach.FullName;

            var type = appointment.AppointmentType.Name;

            lines.Add($"{day}, {appointmentTime} - {type} - {coach}");
        }

        return string.Join("\n", lines);
    }

    private string PrepareEmailBody(List<Appointment> clientAppointments)
    {
        var emailBody = new List<string>();

        foreach (var appointment in clientAppointments)
        {
            var day = Enum.GetName(typeof(DayOfWeek), appointment.TimeSlot.Day);

            var appointmentTime = $"{appointment.TimeSlot.StartTime} - {appointment.TimeSlot.EndTime}";

            var coach = appointment.Coaches.First().Coach.FullName;

            var type = appointment.AppointmentType.Name;

            var appointmentStrings = $"{day}, {appointmentTime} - {type} - {coach}";

            emailBody.Add(appointmentStrings);
        }

        return string.Join("<br/>", emailBody);
    }

    private async Task<Appointment?> CreateAppointment(RecurringAppointment recurring, AppointmentStatus status,
        CancellationToken cancellationToken)
    {
        var appointmentDate = DateExtensions.GetNextWeekday(recurring.TimeSlot.Day);

        var startTime = DateExtensions.CombineDateAndTime(appointmentDate, recurring.TimeSlot.StartTime);

        var endTime = DateExtensions.CombineDateAndTime(appointmentDate, recurring.TimeSlot.EndTime);

        var appointmentClients = recurring.Clients.Select(x => x.Client).ToList();

        if (!_appointmentService.CheckClientsCredits(appointmentClients, cancellationToken))
            return null;

        if (!await _appointmentService.CheckFreeSlot(startTime, recurring.TimeSlot, cancellationToken))
            return null;

        var newAppointment = new Appointment
        {
            AppointmentStatus = status,
            AppointmentType = recurring.AppointmentType,
            TimeSlot = recurring.TimeSlot,
            StartTime = startTime,
            EndTime = endTime
        };

        var clients = recurring.Clients.Select(x => x.Client).ToList();

        var coaches = new List<Coach> { recurring.Coach };

        newAppointment.Clients = clients
            .Select(client => new ClientAppointment
            {
                ClientId = client.Id,
                Client = client,
                Appointment = newAppointment
            })
            .ToList();

        newAppointment.Coaches = coaches
            .Select(coach => new CoachAppointment
            {
                CoachId = coach.Id,
                Coach = coach,
                Appointment = newAppointment
            })
            .ToList();

        return newAppointment;
    }
}