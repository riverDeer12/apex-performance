using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record DeleteAppointmentResponse(
    Guid Id
);

public class DeleteAppointmentEndpoint : EndpointWithoutRequest<DeleteAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IClientService _clientService;
    private readonly IAppointmentService _appointmentService;

    public DeleteAppointmentEndpoint(ApexPerformanceContext context, IClientService clientService,
        IAppointmentService appointmentService)
    {
        _context = context;
        _clientService = clientService;
        _appointmentService = appointmentService;
    }

    public override void Configure()
    {
        Delete("api/appointments/{id}");
        Permissions(nameof(UserPermissions.CanDeleteAppointment));
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment =
            await _context.Appointments.Include(appointment => appointment.Clients)
                .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.NotFound);

        appointment.Delete();

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var clients = await _clientService.GetClientsByAppointmentId(appointmentId, cancellationToken);
            
        await  SendAsync(
            new DeleteAppointmentResponse(appointment.Id),
            cancellation: cancellationToken);
    }
}