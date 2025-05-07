using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record DeleteAppointmentResponse(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime
    );

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

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment =
            await _context.Appointments
                .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.NotFound);

        appointment.Delete();

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new DeleteAppointmentResponse(appointment.Id, appointment.StartTime, appointment.EndTime),
            cancellation: cancellationToken);
    }
}