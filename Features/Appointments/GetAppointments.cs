using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public class GetAppointmentsEndpoint : EndpointWithoutRequest<AppointmentsStatusDto>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAppointmentsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/appointments");
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointments = await GetAppointmentsForUser(cancellationToken);

        if (appointments.Count is 0)
        {
            await SendNoContentAsync(cancellation: cancellationToken);
            return;
        }

        var approvedAppointments = new List<AppointmentDataDto>();

        var pendingAppointments = new List<AppointmentDataDto>();

        var inProgressAppointments = new List<AppointmentDataDto>();

        foreach (var appointment in appointments)
        {
            var appointmentClientsResponse = appointment.Clients
                .Select(appointmentClient =>
                    new PersonDataDto(appointmentClient.Client.Id, appointmentClient.Client.FirstName,
                        appointmentClient.Client.LastName))
                .ToList();

            var appointmentCoachesResponse = appointment.Coaches
                .Select(coach => new PersonDataDto(coach.CoachId, coach.Coach.FirstName, coach.Coach.LastName))
                .ToList();

            var appointmentTypeResponse = new CatalogDataDto(appointment.AppointmentType.Id,
                appointment.AppointmentType.Name, appointment.AppointmentType.Description);
            
            var appointmentStatusResponse = new CatalogDataDto(appointment.AppointmentStatus.Id,
                appointment.AppointmentStatus.Name, appointment.AppointmentStatus.Description);

            var appointmentResponse = new AppointmentDataDto(appointment.Id,
                appointment.StartTime, appointment.EndTime, appointmentTypeResponse, appointmentStatusResponse,
                appointmentClientsResponse, appointmentCoachesResponse);

            switch (appointment.AppointmentStatus.Name)
            {
                case BusinessStatuses.Approved:
                    approvedAppointments.Add(appointmentResponse);
                    continue;
                case BusinessStatuses.Pending:
                    pendingAppointments.Add(appointmentResponse);
                    continue;
                case BusinessStatuses.InProgress:
                    inProgressAppointments.Add(appointmentResponse);
                    continue;
            }
        }

        await SendAsync(
            new AppointmentsStatusDto(approvedAppointments, pendingAppointments, inProgressAppointments),
            cancellation: cancellationToken);
    }

    private async Task<List<Appointment>> GetAppointmentsForUser(CancellationToken cancellationToken)
    {
        if (_currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Administrator))
            return await GetAllAppointments(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Coach))
            return await GetCoachAppointments(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Client))
            return await GetClientAppointments(cancellationToken);

        return new List<Appointment>();
    }

    private async Task<List<Appointment>> GetAllAppointments(CancellationToken cancellationToken)
    {
        return await _context.Appointments
            .Include(appointment => appointment.Clients)
            .ThenInclude(clientAppointment => clientAppointment.Client)
            .Include(appointment => appointment.AppointmentType)
            .Include(appointment => appointment.AppointmentStatus)
            .Include(appointment => appointment.Coaches)
            .ThenInclude(coachAppointment => coachAppointment.Coach)
            .Include(appointment => appointment.TimeSlot)
            .OrderByDescending(x => x.StartTime)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<Appointment>> GetCoachAppointments(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var appointmentRelations = await _context.CoachAppointments
            .Where(x => x.CoachId == coach.Id)
            .Select(x => x.AppointmentId)
            .ToListAsync(cancellationToken);

        if (appointmentRelations.Count is 0) return [];

        return await _context.Appointments
            .Where(appointment =>
                appointmentRelations.Contains(appointment.Id) &&
                appointment.StartTime > DateTimeOffset.Now && !appointment.IsDeleted)
            .Include(appointment => appointment.AppointmentType)
            .Include(appointment => appointment.AppointmentStatus)
            .Include(appointment => appointment.Coaches)
            .ThenInclude(clientAppointment => clientAppointment.Coach)
            .Include(appointment => appointment.Clients)
            .ThenInclude(coachAppointment => coachAppointment.Client)
            .OrderBy(x => x.StartTime)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Appointment>> GetClientAppointments(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var appointmentRelations = await _context.ClientAppointments
            .Where(x => x.ClientId == client.Id)
            .Select(x => x.AppointmentId)
            .ToListAsync(cancellationToken);

        if (appointmentRelations.Count is 0) return [];

        return await _context.Appointments
            .Where(appointment => appointmentRelations.Contains(appointment.Id) && !appointment.IsDeleted)
            .Include(appointment => appointment.AppointmentType)
            .Include(appointment => appointment.AppointmentStatus)
            .Include(appointment => appointment.Clients)
            .ThenInclude(clientAppointment => clientAppointment.Client)
            .Include(appointment => appointment.Coaches)
            .ThenInclude(coachAppointment => coachAppointment.Coach)
            .OrderByDescending(x => x.StartTime)
            .ToListAsync(cancellationToken);
    }
}