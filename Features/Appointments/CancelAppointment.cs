using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public class CancelAppointmentEndpoint: EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IClientService _clientService;
    private readonly IEmailService _emailService;

    public CancelAppointmentEndpoint(ApexPerformanceContext context, IClientService clientService, 
        IEmailService emailService)
    {
        _context = context;
        _clientService = clientService;
        _emailService = emailService;
    }

    public override void Configure()
    {
        Get("api/appointments/cancel/{id}");
        Permissions(nameof(UserPermissions.CanCancelAppointment));
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment =
            await _context.Appointments
                .Include(appointment => appointment.Clients)
                .ThenInclude(clientAppointment => clientAppointment.Client)
                .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.NotFound);
        
        var appointmentStatus = await _context.AppointmentStatuses
            .FirstOrDefaultAsync(x => x.Name == nameof(BusinessStatuses.Canceled),
                cancellationToken: cancellationToken);

        if (appointmentStatus is null)
            ThrowError(ErrorMessages.NotFound);

        appointment.AppointmentStatus = appointmentStatus;

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var appointmentClients = appointment.Clients.Select(x => x.Client).ToList();

        _emailService.SendAppointmentStatus(appointmentClients, appointment);
        
        await _clientService.AddClientsCredits(appointmentClients, 1, cancellationToken);

        await SendAsync(
            new StatusResponse(appointment.Id, true),
            cancellation: cancellationToken);
    }
}