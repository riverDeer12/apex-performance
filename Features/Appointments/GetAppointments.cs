using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record GetAppointmentResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<AppointmentClientDto> Clients
    );

public class GetAppointmentsEndpoint : EndpointWithoutRequest<List<GetAppointmentResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAppointmentsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }
    
    public override void Configure()
    {
        Get("api/appointments");
        Permissions(nameof(UserPermissions.CanGetAppointments));
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointments = await _context.Appointments
            .Include(appointment => appointment.Clients)
            .ThenInclude(clientAppointment => clientAppointment.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        if (appointments.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var appointmentResponseList = new List<GetAppointmentResponse>();

        foreach (var appointment in appointments)
        {
            var appointmentClientsResponse = appointment
                .Clients
                .Select(client => 
                    new AppointmentClientDto(client.Client.Id, client.Client.FirstName, client.Client.LastName))
                .ToList();

            var appointmentResponse = new GetAppointmentResponse(appointment.Id,
                appointment.StartTime, appointment.EndTime, appointmentClientsResponse);

            appointmentResponseList.Add(appointmentResponse);
        }

        await SendAsync(appointmentResponseList, cancellation: cancellationToken);
    }
}