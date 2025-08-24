using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public record ChangeClientRecurringAppointmentActivityResponse(Guid Id);

public class
    ChangeClientRecurringAppointmentActivityEndpoint : EndpointWithoutRequest<
    ChangeClientRecurringAppointmentActivityResponse>
{
    private readonly ApexPerformanceContext _context;

    public ChangeClientRecurringAppointmentActivityEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/recurring-appointments/{id}/activity");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var recurringAppointmentId = Route<Guid>("id", isRequired: true);

        var recurringAppointment =
            await _context.RecurringAppointments.FirstOrDefaultAsync(
                x => x.Id == recurringAppointmentId,
                cancellationToken: cancellationToken);

        if (recurringAppointment is null)
            ThrowError(ErrorMessages.NotFound);

        recurringAppointment.IsActive = !recurringAppointment.IsActive;

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception(ErrorMessages.SavingError);

        await SendAsync(new ChangeClientRecurringAppointmentActivityResponse(recurringAppointment.Id),
            cancellation: cancellationToken);
    }
}