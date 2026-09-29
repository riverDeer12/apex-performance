using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentLocations;

public record AppointmentLocationResponse(
    Guid Id,
    string Name,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    string? GoogleMapsUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public class GetAppointmentLocationsEndpoint : EndpointWithoutRequest<List<AppointmentLocationResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAppointmentLocationsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointment-locations");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("AppointmentLocations"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var locations = await _context.AppointmentLocations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AppointmentLocationResponse(x.Id, x.Name, x.Address, x.Latitude, x.Longitude,
                x.GoogleMapsUrl, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        await SendAsync(locations, cancellation: cancellationToken);
    }
}
