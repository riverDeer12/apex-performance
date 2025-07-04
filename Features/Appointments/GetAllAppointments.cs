using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record AppointmentCoachDto(
    Guid Id,
    string Fullname
);

public class GetAllAppointmentsEndpoint : EndpointWithoutRequest<List<GetAppointmentResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAllAppointmentsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointments");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointments = await _context.Appointments
            .Include(appointment => appointment.Clients)
            .ThenInclude(clientAppointment => clientAppointment.Client)
            .Include(appointment => appointment.AppointmentType)
            .Include(appointment => appointment.AppointmentStatus)
            .Include(appointment => appointment.Coaches)
            .ThenInclude(coachAppointment => coachAppointment.Coach)
            .OrderByDescending(x => x.StartTime)
            .ToListAsync(cancellationToken: cancellationToken);

        if (appointments.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var appointmentResponseList = new List<GetAppointmentResponse>();

        foreach (var appointment in appointments)
        {
            var appointmentClientsResponse = appointment.Clients
                .Select(client =>
                    new AppointmentClientDto(client.Client.Id, client.Client.FirstName, client.Client.LastName))
                .ToList();
            
            var appointmentCoachesResponse = appointment.Coaches
                .Select(coach => new AppointmentCoachDto(coach.CoachId, coach.Coach.FullName)).ToList();

            var appointmentTypeResponse = new CatalogDataDto(appointment.AppointmentType.Id,
                appointment.AppointmentType.Name, appointment.AppointmentType.Description);

            var appointmentStatusDto = new CatalogDataDto(appointment.AppointmentStatus.Id,
                appointment.AppointmentStatus.Name, appointment.AppointmentStatus.Description);

            var appointmentResponse = new GetAppointmentResponse(appointment.Id,
                appointment.StartTime, appointment.EndTime, appointmentTypeResponse, appointmentStatusDto,
                appointmentClientsResponse, appointmentCoachesResponse);

            appointmentResponseList.Add(appointmentResponse);
        }

        await SendAsync(appointmentResponseList, cancellation: cancellationToken);
    }
}