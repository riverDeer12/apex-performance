using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentRequests;

public record GetAppointmentRequestsResponse(
    Guid Id,
    string Comment,
    CatalogDataDto Type,
    CatalogDataDto Status,
    AppointmentDataDto Appointment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public class GetAppointmentRequestsEndpoint : EndpointWithoutRequest<List<GetAppointmentRequestsResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAppointmentRequestsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/appointment-requests");
        Options(x => x.WithTags("AppointmentRequests"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentRequests = await GetAppointmentRequestsForUser(cancellationToken);

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
            var relatedAppointment = appointments.FirstOrDefault(x => x.Id == appointmentRequest.AppointmentId);

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

            appointmentRequestsResponse.Add(new GetAppointmentRequestsResponse(appointmentRequest.Id,
                appointmentRequest.Comment, requestType, requestStatus, appointmentResponse,
                appointmentRequest.CreatedAt, appointmentRequest.UpdatedAt));
        }

        await SendAsync(appointmentRequestsResponse, cancellation: cancellationToken);
    }
    
    private async Task<List<AppointmentRequest>> GetAppointmentRequestsForUser(CancellationToken cancellationToken)
    {
        if (_currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Administrator))
            return await GetAllAppointmentRequests(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Coach))
            return await GetCoachAppointmentRequests(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Client))
            return await GetClientAppointmentRequests(cancellationToken);

        return new List<AppointmentRequest>();
    }
    
    private async Task<List<AppointmentRequest>> GetAllAppointmentRequests(CancellationToken cancellationToken)
    {
        return await _context.AppointmentRequests
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
    }

    private async Task<List<AppointmentRequest>> GetCoachAppointmentRequests(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var coachClients = await _context.CoachClients.Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToListAsync(cancellationToken: cancellationToken);

        var appointmentRequests = await _context.AppointmentRequests
            .Where(x => coachClients.Contains(x.ClientId))
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

        return appointmentRequests;
    }

    private async Task<List<AppointmentRequest>> GetClientAppointmentRequests(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var appointmentRequests = await _context.AppointmentRequests
            .Where(x => x.ClientId == client.Id)
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

        return appointmentRequests;
    }
}