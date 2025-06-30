using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Dashboard;

public record GetStatisticsResponse(
    List<StatisticsItem> ActiveClients,
    List<StatisticsItem> ActiveCoaches,
    List<StatisticsItem> LowCreditsClients,
    List<StatisticsAppointmentDto> TodayAppointments,
    List<StatisticsAppointmentDto> PendingAppointments,
    List<StatisticsAppointmentDto> InProgressAppointments
);

public record StatisticsItem(
    Guid Id,
    string FullName
);

public record StatisticsAppointmentDto(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<StatisticsItem> Clients
);

public class GetStatisticsEndpoint : EndpointWithoutRequest<GetStatisticsResponse>
{
    private readonly ApexPerformanceContext _context;

    public GetStatisticsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/statistics/administrator");
        Roles([nameof(UserRoles.SuperAdmin), nameof(UserRoles.Administrator)]);
        Options(x => x.WithTags("Statistics"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var activeClients = await _context.Clients
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken: cancellationToken);

        var activeCoaches = await _context.Coaches
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken: cancellationToken);

        var lowCreditClients = await _context.Clients
            .Where(x => !x.IsDeleted && x.Credits <= 0)
            .ToListAsync(cancellationToken: cancellationToken);

        var todayAppointments = await _context.Appointments
            .Where(x => !x.IsDeleted
                        && x.AppointmentStatus.Name == nameof(BusinessStatuses.Approved)
                        && x.StartTime.Date == DateTime.Today)
            .ToListAsync(cancellationToken);

        var pendingAppointments = await _context.Appointments
            .Where(x => !x.IsDeleted && x.AppointmentStatus.Name == nameof(BusinessStatuses.Pending))
            .ToListAsync(cancellationToken);

        var inProgressAppointments = await _context.Appointments
            .Where(x => !x.IsDeleted && x.AppointmentStatus.Name == nameof(BusinessStatuses.InProgress))
            .ToListAsync(cancellationToken);

        var lowCreditStatistics = MapToStatisticsItem(
            lowCreditClients,
            client => client.Id,
            client => $"{client.FirstName} {client.LastName}"
        );

        var activeClientsStatistics = MapToStatisticsItem(
            activeClients,
            client => client.Id,
            client => $"{client.FirstName} {client.LastName}"
        );

        var activeCoachesStatistics = MapToStatisticsItem(
            activeCoaches,
            coach => coach.Id,
            coach => $"{coach.FirstName} {coach.LastName}"
        );

        await SendAsync(
            new GetStatisticsResponse(
                activeClientsStatistics,
                activeCoachesStatistics,
                lowCreditStatistics,
                MapToAppointmentStatisticsItem(todayAppointments),
                MapToAppointmentStatisticsItem(pendingAppointments),
                MapToAppointmentStatisticsItem(inProgressAppointments)),
            cancellation:
            cancellationToken);
    }

    private static List<StatisticsItem> MapToStatisticsItem<T>(
        List<T> items,
        Func<T, Guid> idSelector,
        Func<T, string> nameSelector)
    {
        return items.Select(item => new StatisticsItem(
            idSelector(item),
            nameSelector(item)
        )).ToList();
    }

    private static List<StatisticsAppointmentDto> MapToAppointmentStatisticsItem(
        List<Appointment> appointments)
    {
        return appointments.Select(item => new StatisticsAppointmentDto(
            item.Id,
            item.StartTime,
            item.EndTime,
            item.Clients
                .Select(x => new StatisticsItem(x.ClientId, x.Client.FullName))
                .ToList()
        )).ToList();
    }
}