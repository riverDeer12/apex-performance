using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentStatuses;

public record DeleteAppointmentStatusResponse(
    Guid Id
);

public class DeleteAppointmentStatusEndpoint : EndpointWithoutRequest<DeleteAppointmentStatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteAppointmentStatusEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/appointment-statuses/{id}");
        Roles(nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator));
        Options(x => x.WithTags("AppointmentStatuses"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentStatusId = Route<Guid>("id", isRequired: true);

        var appointmentStatus =
            await _context.AppointmentStatuses
                .FirstOrDefaultAsync(x => x.Id == appointmentStatusId, cancellationToken: cancellationToken);

        if (appointmentStatus is null)
            ThrowError(ErrorMessages.NotFound);

        appointmentStatus.Delete();

        _context.AppointmentStatuses.Update(appointmentStatus);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new DeleteAppointmentStatusResponse(appointmentStatus.Id),
            cancellation: cancellationToken);
    }
}