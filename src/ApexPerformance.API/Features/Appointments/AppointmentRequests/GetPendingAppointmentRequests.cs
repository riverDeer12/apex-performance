using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Services.Interfaces;
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
    private readonly ICurrentUserService _currentUserService;

    public GetPendingAppointmentRequestsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/appointment-requests/pending");
        Options(x => x.WithTags("AppointmentRequests"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentRequests = await GetPendingAppointmentRequestsForUser(cancellationToken);

        if (appointmentRequests.Count is 0)
        {
            await SendAsync([],cancellation: cancellationToken);
            return;
        }

        var appointments = appointmentRequests
            .Select(x => x.Appointment)
            .ToList();

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
                ThrowError(ErrorCodes.NotFound);

            var requestSenderResponse =
                new PersonDataDto(requestSender.Id, requestSender.FirstName,
                    requestSender.LastName, requestSender.FullName);

            var relatedAppointment = appointments
                .FirstOrDefault(x => x.Id == appointmentRequest.AppointmentId);

            if (relatedAppointment is null)
                ThrowError(ErrorCodes.NotFound);

            var timeSlot = new CatalogDataDto(relatedAppointment.TimeSlot.Id, relatedAppointment.TimeSlot.Name,
                relatedAppointment.TimeSlot.Description);

            var appointmentType = new CatalogDataDto(relatedAppointment.AppointmentType.Id,
                relatedAppointment.AppointmentType.Name,
                relatedAppointment.AppointmentType.Description);

            var appointmentStatus = new CatalogDataDto(relatedAppointment.AppointmentStatus.Id,
                relatedAppointment.AppointmentStatus.Name,
                relatedAppointment.AppointmentStatus.Description);

            var clients = relatedAppointment.Clients
                .Select(x => new PersonDataDto(x.Client.Id, x.Client.FirstName,
                    x.Client.LastName, x.Client.FullName))
                .ToList();

            var coaches = relatedAppointment.Coaches
                .Select(x => new PersonDataDto(x.Coach.Id, x.Coach.FirstName,
                    x.Coach.LastName, x.Coach.FullName))
                .ToList();

            var appointmentResponse = new AppointmentDataDto(
                relatedAppointment.Id, relatedAppointment.StartTime, relatedAppointment.EndTime, appointmentType,
                appointmentStatus,
                clients, coaches, timeSlot);

            appointmentRequestsResponse.Add(new GetPendingAppointmentRequestResponse(appointmentRequest.Id,
                appointmentRequest.Comment, requestSenderResponse, requestType, appointmentResponse));
        }

        await SendAsync(appointmentRequestsResponse, cancellation: cancellationToken);
    }

    private async Task<List<AppointmentRequest>> GetPendingAppointmentRequestsForUser(
        CancellationToken cancellationToken)
    {
        if (_currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Administrator))
            return await GetAllPendingAppointmentRequests(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Coach))
            return await GetCoachPendingAppointmentRequests(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Client))
            return await GetClientPendingAppointmentRequests(cancellationToken);

        return new List<AppointmentRequest>();
    }

    private async Task<List<AppointmentRequest>> GetAllPendingAppointmentRequests(
        CancellationToken cancellationToken)
    {
        return await _context.AppointmentRequests
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
            .Include(appointmentRequest => appointmentRequest.Appointment)
            .ThenInclude(appointment => appointment.TimeSlot)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<AppointmentRequest>> GetCoachPendingAppointmentRequests(
        CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorCodes.NotFound);

        var coachClientIds = await _context.CoachClients
            .Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToListAsync(cancellationToken: cancellationToken);

        if (coachClientIds.Count is 0) return [];

        return await _context.AppointmentRequests
            .Where(x => x.AppointmentRequestStatus.Name == BusinessStatuses.Pending &&
                        coachClientIds.Contains(x.ClientId))
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
            .Include(appointmentRequest => appointmentRequest.Appointment)
            .ThenInclude(appointment => appointment.TimeSlot)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<AppointmentRequest>> GetClientPendingAppointmentRequests(
        CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound);

        return await _context.AppointmentRequests
            .Where(x => x.AppointmentRequestStatus.Name == BusinessStatuses.Pending &&
                        x.ClientId == client.Id)
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
            .Include(appointmentRequest => appointmentRequest.Appointment)
            .ThenInclude(appointment => appointment.TimeSlot)
            .ToListAsync(cancellationToken: cancellationToken);
    }
}