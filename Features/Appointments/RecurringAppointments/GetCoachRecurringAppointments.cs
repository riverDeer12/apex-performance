using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public class GetCoachRecurringAppointmentsEndpoint : EndpointWithoutRequest<List<RecurringAppointmentDto>>
{
    private readonly ApexPerformanceContext _context;

    private readonly ICurrentUserService _currentUserService;

    public GetCoachRecurringAppointmentsEndpoint(ApexPerformanceContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/recurring-appointments/coach");
        Roles(UserRoles.Coach);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var recurringAppointments = await _context.RecurringAppointments
            .Where(x => !x.IsDeleted && x.CoachId == coach.Id)
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
                    new PersonDataDto(x.Coach.Id, x.Coach.FirstName, x.Coach.LastName), x.TimeSlot)).ToList(),
            cancellation: cancellationToken);
    }
}