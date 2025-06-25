using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record GetAppointmentResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    CatalogDataDto Type,
    CatalogDataDto Status,
    List<AppointmentClientDto> Clients
);

public record AppointmentsByDayDto(
    DateTimeOffset Day,
    List<GetAppointmentResponse> Appointments
);

public class GetAppointmentsByDayEndpoint : EndpointWithoutRequest<List<AppointmentsByDayDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetAppointmentsByDayEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointments/by-day");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointments = await _context.Appointments
            .Include(appointment => appointment.Clients)
            .ThenInclude(clientAppointment => clientAppointment.Client)
            .Include(appointment => appointment.AppointmentType).Include(appointment => appointment.AppointmentStatus)
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

            var appointmentTypeResponse = new CatalogDataDto(appointment.AppointmentType.Id,
                appointment.AppointmentType.Name, appointment.AppointmentType.Description);

            var appointmentStatusResponse = new CatalogDataDto(appointment.AppointmentStatus.Id,
                appointment.AppointmentStatus.Name, appointment.AppointmentStatus.Description);

            var appointmentResponse = new GetAppointmentResponse(appointment.Id,
                appointment.StartTime, appointment.EndTime, appointmentTypeResponse, appointmentStatusResponse,
                appointmentClientsResponse);

            appointmentResponseList.Add(appointmentResponse);
        }

        var appointmentsByDay = GroupAppointmentsByDay(appointmentResponseList);

        await SendAsync(appointmentsByDay, cancellation: cancellationToken);
    }

    private List<AppointmentsByDayDto> GroupAppointmentsByDay(List<GetAppointmentResponse> appointmentResponseList)
    {
        var itemsByDay = appointmentResponseList
            .GroupBy(item => item.StartTime.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        return itemsByDay.Select(x => new AppointmentsByDayDto(x.Key, x.Value)).ToList();
    }
}