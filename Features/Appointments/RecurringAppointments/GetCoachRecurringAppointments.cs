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
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var recurringAppointments =
            await _context.RecurringAppointments.Where(x => x.CoachId == coach.Id)
                .Include(recurringAppointment => recurringAppointment.Coach)
                .Include(recurringAppointment => recurringAppointment.Clients)
                .ThenInclude(clientRecurringAppointment => clientRecurringAppointment.Client)
                .Include(recurringAppointment => recurringAppointment.TimeSlot)
                .Include(recurringAppointment => recurringAppointment.AppointmentType)
                .ToListAsync(cancellationToken: cancellationToken);

        if (recurringAppointments.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(recurringAppointments.Select(x =>
                    new RecurringAppointmentDto(
                        x.Id,
                        x.Clients.Select(clientRecurringAppointment =>
                            new PersonDataDto(clientRecurringAppointment.Client.Id,
                                clientRecurringAppointment.Client.FirstName,
                                clientRecurringAppointment.Client.LastName)).ToList(),
                        new PersonDataDto(x.Coach.Id, x.Coach.FirstName,
                            x.Coach.LastName),
                        new TimeSlotDto(x.TimeSlot.Id, x.TimeSlot.Name,
                            x.TimeSlot.Day,
                            x.TimeSlot.StartTime, x.TimeSlot.EndTime),
                        x.IsActive,
                        new CatalogDataDto(x.AppointmentType.Id, x.AppointmentType.Name, x.AppointmentType.Description)))
                .DistinctBy(dto => dto.Id)
                .OrderBy(x => x.TimeSlot.Day)
                .ThenBy(x => x.TimeSlot.StartTime)
                .ToList(),
            cancellation:
            cancellationToken);
    }
}