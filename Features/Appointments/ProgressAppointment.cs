using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record ProgressAppointmentResponse(
    Guid Id,
    bool InProgress
);

public class ProgressAppointmentEndpoint : EndpointWithoutRequest<ProgressAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;

    public ProgressAppointmentEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointments/progress/{id}");
        Permissions(UserPermissions.CanProgressAppointment);
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

        var appointmentStatus = await _context.AppointmentStatuses
            .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.InProgress),
                cancellationToken: cancellationToken);

        if (appointmentStatus is null)
            ThrowError(ErrorMessages.NotFound);

        appointment.AppointmentStatus = appointmentStatus;

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new ProgressAppointmentResponse(appointment.Id, true),
            cancellation: cancellationToken);
    }
}