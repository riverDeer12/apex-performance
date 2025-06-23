using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record GetAppointmentsByClientResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    CatalogDataDto AppointmentType
);

public class GetAppointmentsByClientEndpoint : EndpointWithoutRequest<List<GetAppointmentsByClientResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAppointmentsByClientEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
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
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var clientAppointments = await _context.Appointments
            .Where(appointment => appointmentRelations.Contains(appointment.Id) &&
                                  appointment.AppointmentStatus.Name == nameof(BusinessStatuses.Approved))
            .Include(appointment => appointment.AppointmentType)
            .ToListAsync(cancellationToken);

        if (clientAppointments.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(clientAppointments
            .Select(x => new GetAppointmentsByClientResponse(x.Id, x.StartTime, x.EndTime,
                new CatalogDataDto(x.AppointmentType.Id, x.AppointmentType.Name, x.AppointmentType.Description)))
            .ToList(), cancellation: cancellationToken);
    }
}