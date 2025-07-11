using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public record ApproveAppointmentResponse(
    Guid Id,
    bool IsApproved
);

public class ApproveAppointmentEndpoint : EndpointWithoutRequest<ApproveAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IEmailService _emailService;
    private readonly IClientService _clientService;

    public ApproveAppointmentEndpoint(ApexPerformanceContext context, IEmailService emailService,
        IClientService clientService)
    {
        _context = context;
        _emailService = emailService;
        _clientService = clientService;
    }

    public override void Configure()
    {
        Get("api/appointments/approve/{id}");
        Permissions(nameof(UserPermissions.CanApproveAppointment));
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
            .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.Approved),
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

        await _clientService.RemoveClientsCredits(clients, 1, cancellationToken);

        await SendAsync(
            new ApproveAppointmentResponse(appointment.Id, true),
            cancellation: cancellationToken);
    }
}