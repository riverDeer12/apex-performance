using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public record DeleteRecurringAppointmentResponse(Guid Id);

public class DeleteRecurringAppointmentEndpoint : EndpointWithoutRequest<DeleteRecurringAppointmentResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteRecurringAppointmentEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/recurring-appointments/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var recurringAppointmentId = Route<Guid>("id", isRequired: true);

        var recurringAppointment =
            await _context.RecurringAppointments
                .FirstOrDefaultAsync(x => x.Id == recurringAppointmentId,
                    cancellationToken: cancellationToken);

        if (recurringAppointment is null)
            ThrowError(ErrorMessages.NotFound);

        recurringAppointment.Delete();

        _context.RecurringAppointments.Update(recurringAppointment);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new DeleteRecurringAppointmentResponse(recurringAppointment.Id),
            cancellation: cancellationToken);
    }
}