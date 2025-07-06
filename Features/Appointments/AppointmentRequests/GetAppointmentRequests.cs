using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentRequests;

public record GetAppointmentRequestsResponse(
    Guid Id,
    string Comment,
    CatalogDataDto Type,
    CatalogDataDto Status,
    AppointmentDataDto Appointment
);

public class GetAppointmentRequestsEndpoint : EndpointWithoutRequest<List<GetAppointmentRequestsResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAppointmentRequestsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointment-requests");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("AppointmentRequests"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentRequests = await _context.AppointmentRequests
            .Include(appointmentRequest => appointmentRequest.Appointment)
            .ThenInclude(appointment => appointment.AppointmentStatus)
            .Include(appointmentRequest => appointmentRequest.Appointment)
            .ThenInclude(appointment => appointment.AppointmentType)
            .Include(appointmentRequest => appointmentRequest.Appointment)
            .ThenInclude(appointment => appointment.Clients)
            .ThenInclude(clientAppointment => clientAppointment.Client)
            .Include(appointmentRequest => appointmentRequest.Appointment)
            .ThenInclude(appointment => appointment.Coaches)
            .ThenInclude(coachAppointment => coachAppointment.Coach)
            .Include(appointmentRequest => appointmentRequest.AppointmentRequestType)
            .Include(appointmentRequest => appointmentRequest.AppointmentRequestStatus)
            .ToListAsync(cancellationToken: cancellationToken);

        if (appointmentRequests.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var appointments = appointmentRequests
            .Select(x => x.Appointment).ToList();

        var appointmentRequestsResponse = new List<GetAppointmentRequestsResponse>();

        foreach (var appointmentRequest in appointmentRequests)
        {
            var relatedAppointment = appointments.FirstOrDefault(x => x.Id == appointmentRequest.Id);

            if (relatedAppointment is null)
                ThrowError(ErrorMessages.NotFound);

            var requestType = new CatalogDataDto(appointmentRequest.AppointmentRequestType.Id,
                appointmentRequest.AppointmentRequestType.Name,
                appointmentRequest.AppointmentRequestType.Description);

            var requestStatus = new CatalogDataDto(appointmentRequest.AppointmentRequestStatus.Id,
                appointmentRequest.AppointmentRequestStatus.Name,
                appointmentRequest.AppointmentRequestStatus.Description);
            
            var appointmentType = new CatalogDataDto(relatedAppointment.AppointmentType.Id,
                relatedAppointment.AppointmentType.Name,
                relatedAppointment.AppointmentType.Description);

            var clients = relatedAppointment.Clients
                .Select(x => new PersonDataDto(x.Client.Id, x.Client.FirstName, x.Client.LastName))
                .ToList();

            var coaches = relatedAppointment.Coaches
                .Select(x => new PersonDataDto(x.Coach.Id, x.Coach.FirstName, x.Coach.LastName))
                .ToList();

            var appointmentResponse = new AppointmentDataDto(
                relatedAppointment.Id, relatedAppointment.StartTime, relatedAppointment.EndTime, appointmentType,
                clients, coaches);

            appointmentRequestsResponse.Add(new GetAppointmentRequestsResponse(appointmentRequest.Id,
                appointmentRequest.Comment, requestType, requestStatus, appointmentResponse));
        }

        await SendAsync(appointmentRequestsResponse, cancellation: cancellationToken);
    }
}