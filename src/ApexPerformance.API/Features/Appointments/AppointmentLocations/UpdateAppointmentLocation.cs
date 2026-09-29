using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentLocations;

public class UpdateAppointmentLocationEndpoint
    : Endpoint<AppointmentLocationRequest, AppointmentLocationResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateAppointmentLocationEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/appointment-locations/{id}");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("AppointmentLocations"));
    }

    public override async Task HandleAsync(AppointmentLocationRequest request,
        CancellationToken cancellationToken)
    {
        var locationId = Route<Guid>("id", isRequired: true);

        var location = await _context.AppointmentLocations
            .FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken);

        if (location is null)
            ThrowError(ErrorCodes.NotFound);

        location.Name = request.Name.Trim();
        location.Address = AppointmentLocationValidation.Clean(request.Address);
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.GoogleMapsUrl = AppointmentLocationValidation.Clean(request.GoogleMapsUrl);

        // Marked as modified so saving without changes still succeeds.
        _context.Entry(location).State = EntityState.Modified;

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new AppointmentLocationResponse(location.Id, location.Name, location.Address,
                location.Latitude, location.Longitude, location.GoogleMapsUrl, location.CreatedAt, location.UpdatedAt),
            cancellation: cancellationToken);
    }
}
