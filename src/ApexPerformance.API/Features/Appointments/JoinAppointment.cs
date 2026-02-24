using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments;

public class JoinAppointmentEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IEmailService _emailService;
    private readonly IClientService _clientService;

    public JoinAppointmentEndpoint(IEmailService emailService,
        ApexPerformanceContext context, IClientService clientService)
    {
        _emailService = emailService;
        _context = context;
        _clientService = clientService;
    }

    public override void Configure()
    {
        Post("api/appointments/join/{id}");
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentRequestId = Route<Guid>("id", isRequired: true);

        var appointmentRequest =
            await _context.AppointmentRequests
                .Include(appointmentRequest => appointmentRequest.Client)
                .FirstOrDefaultAsync(x => x.Id == appointmentRequestId, cancellationToken: cancellationToken);

        if (appointmentRequest is null)
            ThrowError("Appointment Request is not found.");

        var approvedStatus =
            await _context.AppointmentRequestStatuses.FirstOrDefaultAsync(x => x.Name == BusinessStatuses.Approved,
                cancellationToken: cancellationToken);

        if (approvedStatus is null)
            ThrowError("Approved Status is not found.");

        var appointment =
            await _context.Appointments
                .Include(appointment => appointment.Clients)
                .ThenInclude(clientAppointment => clientAppointment.Client)
                .Include(appointment => appointment.TimeSlot)
                .FirstOrDefaultAsync(x => x.Id == appointmentRequest.AppointmentId,
                    cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError("Appointment Request is not found.");

        appointmentRequest.AppointmentRequestStatus = approvedStatus;
        
        var appointmentExistingClients = appointment.Clients.Select(x => x.Client).ToList();

        var joiningClient = new ClientAppointment
        {
            ClientId = appointmentRequest.Client.Id,
            Client = appointmentRequest.Client,
            AppointmentId = appointment.Id,
            Appointment = appointment
        };

        appointment.Clients.Add(joiningClient);

        _context.Appointments.Update(appointment);

        _context.AppointmentRequests.Update(appointmentRequest);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await _clientService.RemoveClientsCredits([appointmentRequest.Client], 1, cancellationToken);
        
        _emailService.SendJoinedAppointmentEmail(appointment, appointmentExistingClients,
            appointmentRequest.Client);

        await SendAsync(new StatusResponse(appointment.Id, true), cancellation: cancellationToken);
    }
}