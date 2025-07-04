using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record GetClientAppointmentsResponse(
    List<AppointmentDataDto> ApprovedAppointments,
    List<AppointmentDataDto> PendingAppointments,
    List<AppointmentDataDto> InProgressAppointments
);

public class GetClientAppointmentsEndpoint : EndpointWithoutRequest<GetClientAppointmentsResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetClientAppointmentsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/appointments/client");
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var appointmentRelations = await _context.ClientAppointments
            .Where(x => x.ClientId == client.Id)
            .Select(x => x.AppointmentId)
            .ToListAsync(cancellationToken);

        if (appointmentRelations.Count is 0)
        {
            await SendAsync(new GetClientAppointmentsResponse(
                Array.Empty<AppointmentDataDto>().ToList(),
                Array.Empty<AppointmentDataDto>().ToList(),
                Array.Empty<AppointmentDataDto>().ToList()
            ), cancellation: cancellationToken);
            return;
        }

        var clientAppointments = await _context.Appointments
            .Where(appointment => appointmentRelations.Contains(appointment.Id))
            .Include(appointment => appointment.AppointmentType)
            .Include(appointment => appointment.AppointmentStatus)
            .Include(appointment => appointment.Clients)
            .ThenInclude(clientAppointment => clientAppointment.Client)
            .Include(appointment => appointment.Coaches)
            .ThenInclude(coachAppointment => coachAppointment.Coach)
            .OrderByDescending(x => x.StartTime)
            .ToListAsync(cancellationToken);

        if (clientAppointments.Count is 0)
        {
            await SendAsync(new GetClientAppointmentsResponse(
                Array.Empty<AppointmentDataDto>().ToList(),
                Array.Empty<AppointmentDataDto>().ToList(),
                Array.Empty<AppointmentDataDto>().ToList()), cancellation: cancellationToken);
            return;
        }

        var approvedAppointments = new List<AppointmentDataDto>();

        var pendingAppointments = new List<AppointmentDataDto>();

        var inProgressAppointments = new List<AppointmentDataDto>();

        foreach (var appointment in clientAppointments)
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

            var appointmentResponse = new AppointmentDataDto(appointment.Id,
                appointment.StartTime, appointment.EndTime, appointmentTypeResponse,
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
            new GetClientAppointmentsResponse(approvedAppointments, pendingAppointments, inProgressAppointments),
            cancellation: cancellationToken);
    }
}