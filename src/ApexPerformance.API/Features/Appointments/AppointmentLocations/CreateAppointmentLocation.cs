using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;

namespace ApexPerformance.API.Features.Appointments.AppointmentLocations;

public class CreateAppointmentLocationEndpoint
    : Endpoint<AppointmentLocationRequest, AppointmentLocationResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateAppointmentLocationEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/appointment-locations");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("AppointmentLocations"));
    }

    public override async Task HandleAsync(AppointmentLocationRequest request,
        CancellationToken cancellationToken)
    {
        var location = new AppointmentLocation
        {
            Name = request.Name.Trim(),
            Address = AppointmentLocationValidation.Clean(request.Address),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            GoogleMapsUrl = AppointmentLocationValidation.Clean(request.GoogleMapsUrl)
        };

        _context.AppointmentLocations.Add(location);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new AppointmentLocationResponse(location.Id, location.Name, location.Address,
                location.Latitude, location.Longitude, location.GoogleMapsUrl, location.CreatedAt, location.UpdatedAt),
            cancellation: cancellationToken);
    }
}
