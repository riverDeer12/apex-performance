using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentRequests;

public class SendJoinRequestEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;

    public SendJoinRequestEndpoint(ICurrentUserService currentUserService, ApexPerformanceContext context,
        IEmailService emailService)
    {
        _currentUserService = currentUserService;
        _context = context;
        _emailService = emailService;
    }

    public override void Configure()
    {
        Get("api/appointment-requests/join/{id}");
        Options(x => x.WithTags("AppointmentRequests"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment =
            await _context.Appointments
                .Include(x => x.Coaches)
                .ThenInclude(x => x.Coach)
                .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError("Appointment is not found.");

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError("Client is not found.");

        var appointmentRequestType =
            await _context.AppointmentRequestTypes.FirstOrDefaultAsync(x =>
                x.Name == nameof(BusinessActions.JoinRequest), cancellationToken: cancellationToken);

        if (appointmentRequestType is null)
            ThrowError("Join Request Type not found.");

        var appointmentRequestStatus =
            await _context.AppointmentRequestStatuses.FirstOrDefaultAsync(x =>
                x.Name == nameof(BusinessStatuses.Pending), cancellationToken: cancellationToken);

        if (appointmentRequestStatus is null)
            ThrowError(ErrorCodes.NotFound);

        var appointmentRequest = new AppointmentRequest
        {
            Appointment = appointment,
            AppointmentId = appointment.Id,
            AppointmentRequestType = appointmentRequestType,
            AppointmentRequestTypeId = appointmentRequestType.Id,
            Comment = "Client wants join request.",
            Client = client,
            ClientId = client.Id,
            AppointmentRequestStatus = appointmentRequestStatus,
            AppointmentRequestStatusId = appointmentRequestStatus.Id
        };

        _context.AppointmentRequests.Add(appointmentRequest);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        BackgroundJob.Enqueue(() =>
            SendJoinRequestNotification(_currentUserService.UserId, appointmentId));

        await SendAsync(new StatusResponse(appointmentRequest.Id, true),
            cancellation: cancellationToken);
    }

    public async Task SendJoinRequestNotification(Guid clientUserId, Guid appointmentId)
    {
        var client = await _context.Clients.SingleAsync(x => x.UserId == clientUserId);

        var appointment = await _context
            .Appointments
            .Include(x => x.Coaches)
            .ThenInclude(x => x.Coach)
            .Include(x => x.Clients)
            .ThenInclude(x => x.Client)
            .SingleAsync(x => x.Id == appointmentId);

        _emailService.SendJoinRequest(client, appointment);
    }
}