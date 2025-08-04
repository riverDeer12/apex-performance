using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public class GetCoachAppointmentsEndpoint : EndpointWithoutRequest<AppointmentsStatusDto>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCoachAppointmentsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/appointments/coach");
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var appointmentRelations = await _context.CoachAppointments
            .Where(x => x.CoachId == coach.Id)
            .Select(x => x.AppointmentId)
            .ToListAsync(cancellationToken);

        if (appointmentRelations.Count is 0)
        {
            await SendAsync(new AppointmentsStatusDto(
                Array.Empty<AppointmentDataDto>().ToList(),
                Array.Empty<AppointmentDataDto>().ToList(),
                Array.Empty<AppointmentDataDto>().ToList()
            ), cancellation: cancellationToken);
            return;
        }

        var coachAppointments = await _context.Appointments
            .Where(appointment => appointmentRelations.Contains(appointment.Id))
            .Include(appointment => appointment.AppointmentType)
            .Include(appointment => appointment.AppointmentStatus)
            .Include(appointment => appointment.Coaches)
            .ThenInclude(clientAppointment => clientAppointment.Coach)
            .Include(appointment => appointment.Clients)
            .ThenInclude(coachAppointment => coachAppointment.Client)
            .OrderByDescending(x => x.StartTime)
            .ToListAsync(cancellationToken);

        if (coachAppointments.Count is 0)
        {
            await SendAsync(new AppointmentsStatusDto(
                Array.Empty<AppointmentDataDto>().ToList(),
                Array.Empty<AppointmentDataDto>().ToList(),
                Array.Empty<AppointmentDataDto>().ToList()), cancellation: cancellationToken);
            return;
        }

        var approvedAppointments = new List<AppointmentDataDto>();

        var pendingAppointments = new List<AppointmentDataDto>();

        var inProgressAppointments = new List<AppointmentDataDto>();

        foreach (var appointment in coachAppointments)
        {
            var appointmentClientsResponse = appointment.Clients
                .Select(appointmentClient =>
                    new PersonDataDto(appointmentClient.Client.Id, appointmentClient.Client.FirstName,
                        appointmentClient.Client.LastName))
                .ToList();

            var appointmentCoachesResponse = appointment.Coaches
                .Select(x => new PersonDataDto(x.CoachId, x.Coach.FirstName, x.Coach.LastName))
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
}