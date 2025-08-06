using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentRequests;

public record GetPendingAppointmentRequestResponse(
    Guid Id,
    string Comment,
    PersonDataDto Sender,
    CatalogDataDto Type,
    AppointmentDataDto Appointment
);

public class GetPendingAppointmentRequestsEndpoint : EndpointWithoutRequest<List<GetPendingAppointmentRequestResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetPendingAppointmentRequestsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointment-requests/pending");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("AppointmentRequests"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentRequests = await _context.AppointmentRequests
            .Where(x => x.AppointmentRequestStatus.Name == BusinessStatuses.Pending)
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
            .Include(appointmentRequest => appointmentRequest.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        if (appointmentRequests.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var appointments = appointmentRequests
            .Select(x => x.Appointment).ToList();

        var appointmentRequestSenders = appointmentRequests.Select(x => x.Client).ToList();

        var appointmentRequestsResponse = new List<GetPendingAppointmentRequestResponse>();

        foreach (var appointmentRequest in appointmentRequests)
        {
            var requestType = new CatalogDataDto(appointmentRequest.AppointmentRequestType.Id,
                appointmentRequest.AppointmentRequestType.Name,
                appointmentRequest.AppointmentRequestType.Description);

            var requestSender = appointmentRequestSenders
                .FirstOrDefault(x => x.Id == appointmentRequest.ClientId);

            if (requestSender is null)
                ThrowError(ErrorMessages.NotFound);

            var requestSenderResponse =
                new PersonDataDto(requestSender.Id, requestSender.FirstName, requestSender.LastName);

            var relatedAppointment = appointments
                .FirstOrDefault(x => x.Id == appointmentRequest.AppointmentId);

            if (relatedAppointment is null)
                ThrowError(ErrorMessages.NotFound);

            var appointmentType = new CatalogDataDto(relatedAppointment.AppointmentType.Id,
                relatedAppointment.AppointmentType.Name,
                relatedAppointment.AppointmentType.Description);

            var appointmentStatus = new CatalogDataDto(relatedAppointment.AppointmentStatus.Id,
                relatedAppointment.AppointmentStatus.Name,
                relatedAppointment.AppointmentStatus.Description);

            var clients = relatedAppointment.Clients
                .Select(x => new PersonDataDto(x.Client.Id, x.Client.FirstName, x.Client.LastName))
                .ToList();

            var coaches = relatedAppointment.Coaches
                .Select(x => new PersonDataDto(x.Coach.Id, x.Coach.FirstName, x.Coach.LastName))
                .ToList();

            var appointmentResponse = new AppointmentDataDto(
                relatedAppointment.Id, relatedAppointment.StartTime, relatedAppointment.EndTime, appointmentType,
                appointmentStatus,
                clients, coaches);

            appointmentRequestsResponse.Add(new GetPendingAppointmentRequestResponse(appointmentRequest.Id,
                appointmentRequest.Comment, requestSenderResponse, requestType, appointmentResponse));
        }

        await SendAsync(appointmentRequestsResponse, cancellation: cancellationToken);
    }
}