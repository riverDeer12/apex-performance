using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
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
    private readonly IEmailService _emailService;

    public DeclineAppointmentEndpoint(ApexPerformanceContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
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
                .Include(appointment => appointment.Clients)
                .ThenInclude(clientAppointment => clientAppointment.Client)
                .Include(appointment => appointment.AppointmentType)
                .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.NotFound);

        var appointmentStatus = await _context.AppointmentStatuses
            .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.Declined),
                cancellationToken: cancellationToken);

        if (appointmentStatus is null)
            ThrowError(ErrorMessages.NotFound);

        appointment.AppointmentStatus = appointmentStatus;

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var clients = appointment.Clients.Select(x => x.Client).ToList();

        _emailService.SendAppointmentStatus(clients, appointment);

        await SendAsync(
            new DeclineAppointmentResponse(appointment.Id, true),
            cancellation: cancellationToken);
    }
}