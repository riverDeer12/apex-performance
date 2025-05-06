using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;

namespace ApexPerformance.API.Features.Appointments;

public record DeleteAppointmentResponse();

public class DeleteAppointmentEndpoint : EndpointWithoutRequest<DeleteAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteAppointmentEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }
    
    public override void Configure()
    {
        Delete("api/appointments/{id}");
        Permissions(nameof(UserPermissions.CanDeleteAppointment));
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await base.HandleAsync(ct);
    }
}