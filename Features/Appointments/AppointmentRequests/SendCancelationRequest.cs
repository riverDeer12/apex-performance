using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentRequests;

public record SendCancelationRequest(string Comment);

public class SendCancelationRequestEndpoint : Endpoint<SendCancelationRequest, StatusResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;

    public SendCancelationRequestEndpoint(ICurrentUserService currentUserService, ApexPerformanceContext context,
        IEmailService emailService)
    {
        _currentUserService = currentUserService;
        _context = context;
        _emailService = emailService;
    }

    public override void Configure()
    {
        Post("api/appointment-requests/cancelation/{id}");
        Options(x => x.WithTags("AppointmentRequests"));
    }

    public override async Task HandleAsync(SendCancelationRequest request, CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var appointmentId = Route<Guid>("id", isRequired: true);

        var appointment = await _context.Appointments
            .Include(appointment => appointment.Coaches)
            .ThenInclude(coachAppointment => coachAppointment.Coach)
            .Include(appointment => appointment.AppointmentStatus)
            .Include(appointment => appointment.Clients)
            .ThenInclude(appointmentClient => appointmentClient.Client)
            .Include(appointment => appointment.AppointmentType)
            .FirstOrDefaultAsync(x => x.Id == appointmentId,
                cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.NotFound);

        if (appointment.AppointmentStatus.Name != BusinessStatuses.Approved)
            ThrowError(ErrorMessages.NotApproved);

        var appointmentRequestType =
            await _context.AppointmentRequestTypes.FirstOrDefaultAsync(x =>
                x.Name == nameof(BusinessActions.CancelationRequest), cancellationToken: cancellationToken);

        if (appointmentRequestType is null)
            ThrowError(ErrorMessages.NotFound);
        
        var appointmentRequestStatus =
            await _context.AppointmentRequestStatuses.FirstOrDefaultAsync(x =>
                x.Name == nameof(BusinessStatuses.Pending), cancellationToken: cancellationToken);

        if (appointmentRequestStatus is null)
            ThrowError(ErrorMessages.NotFound);

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
            ThrowError(ErrorMessages.SavingError);

        _emailService.SendCancelationRequest(client, appointment);

        await SendAsync(new StatusResponse(appointmentRequest.Id, true),
            cancellation: cancellationToken);
    }
}