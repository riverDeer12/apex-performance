using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;

namespace ApexPerformance.API.Features.Appointments;

public record UpdateAppointmentRequest();
public record UpdateAppointmentResponse();

public class UpdateAppointmentEndpoint : Endpoint<UpdateAppointmentRequest, UpdateAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateAppointmentEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }
    
    public override void Configure()
    {
        Put("api/appointments/{id}");
        Permissions(nameof(UserPermissions.CanUpdateAppointment));
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(UpdateAppointmentRequest request, CancellationToken ct)
    {
        await base.HandleAsync(request, ct);
    }
}