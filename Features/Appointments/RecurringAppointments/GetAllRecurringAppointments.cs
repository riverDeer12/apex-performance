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
        var recurringAppointments = await _context.RecurringAppointments
            .Include(recurringAppointment => recurringAppointment.Client)
            .Include(recurringAppointment => recurringAppointment.Coach)
            .Include(recurringAppointment => recurringAppointment.TimeSlot)
            .OrderBy(x => x.TimeSlot.StartTime)
            .ToListAsync(cancellationToken: cancellationToken);

        if (recurringAppointments.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(recurringAppointments.Select(x =>
                    new RecurringAppointmentDto(
                        new PersonDataDto(x.Client.Id, x.Client.FirstName, x.Client.LastName),
                        new PersonDataDto(x.Coach.Id, x.Coach.FirstName, x.Coach.LastName),
                        new TimeSlotDto(x.TimeSlot.Id, x.TimeSlot.Name,
                            x.TimeSlot.Day,
                            x.TimeSlot.StartTime, x.TimeSlot.EndTime)))
                .ToList(),
            cancellation: cancellationToken);
    }
}