using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentRequests;

public class ApproveAppointmentRequestEndpoint : EndpointWithoutRequest<ApproveAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppointmentService _appointmentService;

    public ApproveAppointmentRequestEndpoint(ApexPerformanceContext context,
        ICurrentUserService currentUserService, IAppointmentService appointmentService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _appointmentService = appointmentService;
    }

    public override void Configure()
    {
        Get("api/appointment-requests/approve/{id}");
        Options(x => x.WithTags("AppointmentRequests"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var appointmentRequestId = Route<Guid>("id", isRequired: true);

        var appointmentRequest = await _context.AppointmentRequests
            .Include(appointmentRequest => appointmentRequest.AppointmentRequestStatus)
            .FirstOrDefaultAsync(x => x.Id == appointmentRequestId,
                cancellationToken: cancellationToken);

        if (appointmentRequest is null)
            ThrowError(ErrorMessages.NotFound);

        var requestStatus = appointmentRequest.AppointmentRequestStatus;

        var approvedStatus =
            await _context.AppointmentRequestStatuses.FirstOrDefaultAsync(x =>
                x.Name == nameof(BusinessStatuses.Approved), cancellationToken: cancellationToken);

        if (approvedStatus is null)
            ThrowError(ErrorMessages.NotFound);

        appointmentRequest.AppointmentRequestStatus = approvedStatus;

        _context.AppointmentRequests.Update(appointmentRequest);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(x => x.Id == appointmentRequest.AppointmentId,
                cancellationToken: cancellationToken);

        if (appointment is null)
            ThrowError(ErrorMessages.SavingError);

        _appointmentService.ChangeAppointmentStatus(appointment, requestStatus,
            BusinessStatuses.Canceled, cancellationToken);
    }
}