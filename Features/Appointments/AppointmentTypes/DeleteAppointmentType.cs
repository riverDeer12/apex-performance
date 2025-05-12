using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.AppointmentTypes;

public record DeleteAppointmentTypeResponse(
    Guid Id,
    string Name,
    string Description
);

public class DeleteAppointmentTypeEndpoint : EndpointWithoutRequest<DeleteAppointmentTypeResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteAppointmentTypeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/appointment-types/{id}");
        Permissions(nameof(UserPermissions.CanDeleteAppointmentType));
        Options(x => x.WithTags("AppointmentTypes"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var appointmentTypeId = Route<Guid>("id", isRequired: true);

        var appointmentType =
            await _context.AppointmentTypes
                .FirstOrDefaultAsync(x => x.Id == appointmentTypeId, cancellationToken: cancellationToken);

        if (appointmentType is null)
            ThrowError(ErrorMessages.NotFound);

        appointmentType.Delete();

        _context.AppointmentTypes.Update(appointmentType);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new DeleteAppointmentTypeResponse(appointmentType.Id, appointmentType.Name, appointmentType.Description),
            cancellation: cancellationToken);
    }
}