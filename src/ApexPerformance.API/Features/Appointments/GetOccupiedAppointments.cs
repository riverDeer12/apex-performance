using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record OccupiedAppointmentResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime
);

public class GetOccupiedAppointmentsEndpoint : EndpointWithoutRequest<List<OccupiedAppointmentResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOccupiedAppointmentsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/appointments/occupied");
        Roles(UserRoles.Client);
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound);

        var coachIds = await _context.CoachClients
            .Where(x => x.ClientId == client.Id)
            .Select(x => x.CoachId)
            .ToListAsync(cancellationToken);

        if (coachIds.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        // Only times are returned so clients can see
        // when their coaches are busy without seeing
        // who the other appointments belong to.
        var occupiedAppointments = await _context.Appointments
            .Where(appointment =>
                appointment.StartTime > DateTimeOffset.Now &&
                (appointment.AppointmentStatus.Name == BusinessStatuses.Approved ||
                 appointment.AppointmentStatus.Name == BusinessStatuses.InProgress) &&
                appointment.Coaches.Any(coach => coachIds.Contains(coach.CoachId)) &&
                appointment.Clients.All(appointmentClient => appointmentClient.ClientId != client.Id))
            .OrderBy(appointment => appointment.StartTime)
            .Select(appointment =>
                new OccupiedAppointmentResponse(appointment.Id, appointment.StartTime, appointment.EndTime))
            .ToListAsync(cancellationToken);

        await SendAsync(occupiedAppointments, cancellation: cancellationToken);
    }
}
