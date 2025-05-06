using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;

namespace ApexPerformance.API.Features.Appointments;

public record CreateAppointmentRequest();
public record CreateAppointmentResponse();

public class CreateAppointmentEndpoint: Endpoint<CreateAppointmentRequest, CreateAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateAppointmentEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }
    
    public override void Configure()
    {
        Post("api/appointments");
        Permissions(nameof(UserPermissions.CanCreateAppointment));
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CreateAppointmentRequest request, CancellationToken ct)
    {
        await base.HandleAsync(request, ct);
    }
}