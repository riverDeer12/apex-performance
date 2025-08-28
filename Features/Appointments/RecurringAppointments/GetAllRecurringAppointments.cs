using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public class GetAllRecurringAppointmentsEndpoint : EndpointWithoutRequest<List<RecurringAppointmentDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetAllRecurringAppointmentsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/recurring-appointments/all");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var recurringAppointments = await _context.ClientRecurringAppointments
            .Include(clientRecurringAppointment => clientRecurringAppointment.Client)
            .Include(clientRecurringAppointment => clientRecurringAppointment.RecurringAppointment)
            .ThenInclude(recurringAppointment => recurringAppointment.Coach)
            .Include(clientRecurringAppointment => clientRecurringAppointment.RecurringAppointment)
            .ThenInclude(recurringAppointment => recurringAppointment.TimeSlot)
            .ToListAsync(cancellationToken: cancellationToken);

        if (recurringAppointments.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(recurringAppointments.Select(x =>
                    new RecurringAppointmentDto(
                        x.RecurringAppointmentId,
                        new PersonDataDto(x.Client.Id, x.Client.FirstName, x.Client.LastName),
                        new PersonDataDto(x.RecurringAppointment.Coach.Id, x.RecurringAppointment.Coach.FirstName,
                            x.RecurringAppointment.Coach.LastName),
                        new TimeSlotDto(x.RecurringAppointment.TimeSlot.Id, x.RecurringAppointment.TimeSlot.Name,
                            x.RecurringAppointment.TimeSlot.Day,
                            x.RecurringAppointment.TimeSlot.StartTime, x.RecurringAppointment.TimeSlot.EndTime),
                        x.RecurringAppointment.IsActive))
                .ToList(),
            cancellation: cancellationToken);
    }
}