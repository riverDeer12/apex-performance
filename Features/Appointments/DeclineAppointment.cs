using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record DeclineAppointmentResponse(
    Guid Id,
    bool IsDeclined
);

public class DeclineAppointmentEndpoint : EndpointWithoutRequest<DeclineAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeclineAppointmentEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointments/decline/{id}");
        Permissions(nameof(UserPermissions.CanDeclineAppointment));
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
            .FirstOrDefaultAsync(x => x.Name == nameof(Constants.BusinessStatuses.Declined),
                cancellationToken: cancellationToken);

        if (appointmentStatus is null)
            ThrowError(ErrorMessages.NotFound);

        appointment.AppointmentStatus = appointmentStatus;

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new DeclineAppointmentResponse(appointment.Id, true),
            cancellation: cancellationToken);
    }
}