using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentLocations;

/// <summary>
/// Soft delete, appointments keep their
/// link to the location for history.
/// </summary>
public class DeleteAppointmentLocationEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteAppointmentLocationEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/appointment-locations/{id}");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("AppointmentLocations"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var locationId = Route<Guid>("id", isRequired: true);

        var location = await _context.AppointmentLocations
            .FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken);

        if (location is null)
            ThrowError(ErrorCodes.NotFound);

        location.Delete();

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new StatusResponse(location.Id, true), cancellation: cancellationToken);
    }
}
