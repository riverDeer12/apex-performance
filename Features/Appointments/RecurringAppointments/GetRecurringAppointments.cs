using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public class GetRecurringAppointmentsEndpoint : EndpointWithoutRequest<List<RecurringAppointmentDto>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRecurringAppointmentsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/recurring-appointments");
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var recurringAppointments = await GetAppointmentRequestsForUser(cancellationToken);

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
                        new CatalogDataDto(x.AppointmentType.Id,
                            x.AppointmentType.Name,
                            x.AppointmentType.Description)))
                .ToList(),
            cancellation: cancellationToken);
    }

    private async Task<List<RecurringAppointment>> GetAppointmentRequestsForUser(CancellationToken cancellationToken)
    {
        if (_currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Administrator))
            return await GetAllRecurringAppointments(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Coach))
            return await GetCoachRecurringAppointments(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Client))
            return await GetClientRecurringAppointments(cancellationToken);

        return new List<RecurringAppointment>();
    }

    private async Task<List<RecurringAppointment>> GetAllRecurringAppointments(CancellationToken cancellationToken)
    {
        return await _context.RecurringAppointments
            .Include(recurringAppointment => recurringAppointment.Coach)
            .Include(recurringAppointment => recurringAppointment.TimeSlot)
            .Include(recurringAppointment => recurringAppointment.AppointmentType)
            .Include(recurringAppointment => recurringAppointment.Clients)
            .ThenInclude(clientRecurringAppointment => clientRecurringAppointment.Client)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<RecurringAppointment>> GetCoachRecurringAppointments(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var coachClientIds = await _context.CoachClients
            .Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToListAsync(cancellationToken: cancellationToken);

        return await _context.RecurringAppointments
            .Where(recurringAppointment => recurringAppointment.Clients
                .Any(clientRecurringAppointment => coachClientIds.Contains(clientRecurringAppointment.ClientId)))
            .Include(recurringAppointment => recurringAppointment.Coach)
            .Include(recurringAppointment => recurringAppointment.TimeSlot)
            .Include(recurringAppointment => recurringAppointment.AppointmentType)
            .Include(recurringAppointment => recurringAppointment.Clients)
            .ThenInclude(clientRecurringAppointment => clientRecurringAppointment.Client)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<RecurringAppointment>> GetClientRecurringAppointments(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        return await _context.RecurringAppointments
            .Where(recurringAppointment => recurringAppointment.Clients
                .Any(clientRecurringAppointment => clientRecurringAppointment.ClientId == client.Id))
            .Include(recurringAppointment => recurringAppointment.Coach)
            .Include(recurringAppointment => recurringAppointment.TimeSlot)
            .Include(recurringAppointment => recurringAppointment.AppointmentType)
            .Include(recurringAppointment => recurringAppointment.Clients)
            .ThenInclude(clientRecurringAppointment => clientRecurringAppointment.Client)
            .ToListAsync(cancellationToken);
    }
}