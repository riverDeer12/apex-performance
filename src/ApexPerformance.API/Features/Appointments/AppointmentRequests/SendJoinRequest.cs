using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentRequests;

public record SendJoinRequest(string Comment);

public class SendJoinRequestEndpoint: Endpoint<SendJoinRequest, StatusResponse>
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
        Post("api/appointment-requests/join/{id}");
        Options(x => x.WithTags("AppointmentRequests"));
    }

    public override async Task HandleAsync(SendJoinRequest request, CancellationToken cancellationToken)
    {
        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment =
            await _context.Appointments
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
            Comment = request.Comment,
            Client = client,
            ClientId = client.Id,
            AppointmentRequestStatus = appointmentRequestStatus,
            AppointmentRequestStatusId = appointmentRequestStatus.Id
        };
        
        _context.AppointmentRequests.Add(appointmentRequest);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        _emailService.SendJoinRequest(client, appointment);

        await SendAsync(new StatusResponse(appointmentRequest.Id, true),
            cancellation: cancellationToken);
    }
}