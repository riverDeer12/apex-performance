using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentStatuses;

public record GetAppointmentStatusResponse(
    Guid Id,
    string Name,
    string Description
);

public class GetAppointmentStatusesEndpoint : EndpointWithoutRequest<List<GetAppointmentStatusResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAppointmentStatusesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointment-statuses");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("AppointmentStatuses"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentStatuses = await _context.AppointmentStatuses
            .ToListAsync(cancellationToken: cancellationToken);

        if (appointmentStatuses.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(appointmentStatuses
            .Select(x => new GetAppointmentStatusResponse(x.Id, x.Name, x.Description))
            .ToList(), cancellation: cancellationToken);
    }
}