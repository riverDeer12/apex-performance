using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentRequests;

public class DeclineAppointmentRequestEndpoint : EndpointWithoutRequest<DeclineAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeclineAppointmentRequestEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/appointment-requests/decline/{id}");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
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
            ThrowError(ErrorMessages.NotFound);
        
        var requestStatus = appointmentRequest.AppointmentRequestStatus;

        var declinedStatus =
            await _context.AppointmentRequestStatuses.FirstOrDefaultAsync(x =>
                x.Name == nameof(BusinessStatuses.Declined), cancellationToken: cancellationToken);

        if (declinedStatus is null)
            ThrowError(ErrorMessages.NotFound);
        
        if (requestStatus.Name == declinedStatus.Name)
            ThrowError(ErrorMessages.AlreadyChanged);

        appointmentRequest.AppointmentRequestStatus = declinedStatus;

        _context.AppointmentRequests.Update(appointmentRequest);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);
        
        await SendAsync(new DeclineAppointmentResponse(appointmentRequest.Id, true), 
            cancellation: cancellationToken);
    }
}