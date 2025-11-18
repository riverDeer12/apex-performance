using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentRequests;

public class ApproveAppointmentRequestEndpoint : EndpointWithoutRequest<ApproveAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IClientService _clientService;

    public ApproveAppointmentRequestEndpoint(ApexPerformanceContext context, IClientService clientService)
    {
        _context = context;
        _clientService = clientService;
    }

    public override void Configure()
    {
        Get("api/appointment-requests/approve/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("AppointmentRequests"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentRequestId = Route<Guid>("id", isRequired: true);

        var appointmentRequest = await _context.AppointmentRequests
            .Include(appointmentRequest => appointmentRequest.AppointmentRequestStatus)
            .FirstOrDefaultAsync(x => x.Id == appointmentRequestId,
                cancellationToken: cancellationToken);

        if (appointmentRequest is null)
            ThrowError(nameof(appointmentRequest) + ErrorMessages.NotFound);

        var requestStatus = appointmentRequest.AppointmentRequestStatus;

        var approvedStatus =
            await _context.AppointmentRequestStatuses.FirstOrDefaultAsync(x =>
                x.Name == nameof(BusinessStatuses.Approved), cancellationToken: cancellationToken);

        if (approvedStatus is null)
            ThrowError(ErrorMessages.NotFound);

        if (requestStatus.Name == approvedStatus.Name)
            ThrowError(ErrorMessages.AlreadyChanged);

        appointmentRequest.AppointmentRequestStatus = approvedStatus;

        _context.AppointmentRequests.Update(appointmentRequest);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var appointment = await _context.Appointments.Include(appointment => appointment.Clients)
            .ThenInclude(clientAppointment => clientAppointment.Client)
            .FirstOrDefaultAsync(x => x.Id == appointmentRequest.AppointmentId,
                cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.NotFound);

        var appointmentClients = appointment.Clients.Select(x => x.Client).ToList();

        await ChangeAppointmentStatusToCanceled(appointment, appointmentClients, cancellationToken);

        await SendAsync(new ApproveAppointmentResponse(appointmentRequest.Id, true),
            cancellation: cancellationToken);
    }

    private async Task ChangeAppointmentStatusToCanceled(Appointment appointment, List<Client> clients,
        CancellationToken cancellationToken)
    {
        var updatedStatus = await _context.AppointmentStatuses.SingleAsync(
            x => x.Name == BusinessStatuses.Canceled, cancellationToken: cancellationToken);

        appointment.AppointmentStatus = updatedStatus;

        _context.Appointments.Update(appointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await _clientService.AddClientsCredits(clients, 1, cancellationToken);
    }
}