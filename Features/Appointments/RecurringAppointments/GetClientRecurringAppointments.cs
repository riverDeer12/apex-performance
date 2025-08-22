using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public class GetClientRecurringAppointmentsEndpoint : EndpointWithoutRequest<List<RecurringAppointmentDto>>
{
    private readonly ApexPerformanceContext _context;

    private readonly ICurrentUserService _currentUserService;

    public GetClientRecurringAppointmentsEndpoint(ApexPerformanceContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/recurring-appointments/client");
        Roles(UserRoles.Client);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var recurringAppointments = await _context.RecurringAppointments
            .Where(x => !x.IsDeleted && x.ClientId == client.Id)
            .Include(recurringAppointment => recurringAppointment.Client)
            .Include(recurringAppointment => recurringAppointment.Coach)
            .Include(recurringAppointment => recurringAppointment.TimeSlot)
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
                            Enum.GetName(typeof(DayOfWeek), x.TimeSlot.Day)!,
                            x.TimeSlot.StartTime, x.TimeSlot.EndTime)))
                .ToList(),
            cancellation: cancellationToken);
    }
}