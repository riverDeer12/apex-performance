using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public class GenerateNextWeekAppointmentsEndpoint : EndpointWithoutRequest<int>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppointmentService _appointmentService;
    private readonly IEmailService _emailService;

    public GenerateNextWeekAppointmentsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IAppointmentService appointmentService, IEmailService emailService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _appointmentService = appointmentService;
        _emailService = emailService;
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
            ThrowError(ErrorMessages.NotFound);

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
            ThrowError(ErrorMessages.NotFound);

        var nextWeekAppointments = new List<Appointment>();

        foreach (var recurring in coachRecurringAppointments)
        {
            var newAppointment = await CreateAppointment(recurring, approvedStatus, cancellationToken);

            if (newAppointment is null) continue;

            var clients = recurring.Clients.Select(x => x.Client).ToList();

            var coaches = new List<Coach> { recurring.Coach };

            await _appointmentService.UpdateClients(clients, newAppointment, cancellationToken);

            await _appointmentService.UpdateCoaches(coaches, newAppointment, cancellationToken);

            nextWeekAppointments.Add(newAppointment);
        }

        var recurringClients = coachRecurringAppointments
            .SelectMany(x => x.Clients.Select(y => y.Client))
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();

        SendNextWeekNotificationEmails(recurringClients, nextWeekAppointments);

        await SendAsync(StatusCodes.Status201Created, cancellation: cancellationToken);
    }

    private void SendNextWeekNotificationEmails(List<Client> recurringClients,
        List<Appointment> nextWeekAppointments)
    {
        var nextWeekAppointmentsIds = nextWeekAppointments.Select(x => x.Id).ToList();

        foreach (var client in recurringClients)
        {
            var clientAppointments = _context.ClientAppointments
                .Where(clientAppointment => clientAppointment.ClientId == client.Id &&
                                            nextWeekAppointmentsIds.Contains(clientAppointment.AppointmentId))
                .Include(clientAppointment => clientAppointment.Appointment)
                .Include(clientAppointment => clientAppointment.Appointment.AppointmentType)
                .Include(clientAppointment => clientAppointment.Appointment.Coaches)
                .ThenInclude(coachAppointment => coachAppointment.Coach)
                .Select(x => x.Appointment)
                .ToList();

            var emailBody = PrepareEmailBody(clientAppointments);
            
            if(!string.IsNullOrEmpty(emailBody))
                _emailService.SendWeekAppointmentsSchedule(client, emailBody);
        }
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

        if (!await _appointmentService.CheckFreeSlot(startTime, endTime, cancellationToken))
            return null;

        var newAppointment = new Appointment
        {
            AppointmentStatus = status,
            AppointmentType = recurring.AppointmentType,
            TimeSlot = recurring.TimeSlot,
            StartTime = startTime,
            EndTime = endTime
        };

        _context.Appointments.Add(newAppointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        return newAppointment;
    }
}