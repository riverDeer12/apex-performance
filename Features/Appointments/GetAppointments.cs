using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;

namespace ApexPerformance.API.Features.Appointments;

public record GetAppointmentResponse();

public class GetAppointmentsEndpoint : EndpointWithoutRequest<List<GetAppointmentResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAppointmentsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }
    
    public override void Configure()
    {
        Get("api/appointments)");
        Permissions(nameof(UserPermissions.CanGetAppointments));
        Options(x => x.WithTags("Appointments)"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await base.HandleAsync(ct);
    }
}