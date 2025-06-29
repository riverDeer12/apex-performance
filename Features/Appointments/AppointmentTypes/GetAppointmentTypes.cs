using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentTypes;

public record GetAppointmentTypeResponse(
    Guid Id,
    string Name
);

public class GetAppointmentTypesEndpoint : EndpointWithoutRequest<List<GetAppointmentTypeResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAppointmentTypesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointment-types");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("AppointmentTypes"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentTypes = await _context.AppointmentTypes
            .ToListAsync(cancellationToken: cancellationToken);

        if (appointmentTypes.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(appointmentTypes
            .Select(x => new GetAppointmentTypeResponse(x.Id, x.Name))
            .ToList(), cancellation: cancellationToken);
    }
}