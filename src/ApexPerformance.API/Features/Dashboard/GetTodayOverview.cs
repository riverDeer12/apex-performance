using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Clients;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Dashboard;

public record GetTodayOverviewRequest(int? InactiveDays);

public record TodayAppointmentDto(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    List<string> Clients
);

public record OverviewClientDto(Guid Id, string FullName);

public record LowCreditsClientDto(Guid Id, string FullName, int Credits);

public record InactiveClientDto(Guid Id, string FullName, DateTimeOffset? LastActivityAt);

public record PendingRequestsDto(int Appointments, int CancelationRequests, int JoinRequests);

public record MonthlyReviewsDueDto(int Year, int Month, DateTimeOffset DueAt, List<OverviewClientDto> MissingClients);

public record GetTodayOverviewResponse(
    List<TodayAppointmentDto> TodayAppointments,
    PendingRequestsDto PendingRequests,
    List<LowCreditsClientDto> LowCreditsClients,
    int InactiveDays,
    List<InactiveClientDto> InactiveClients,
    MonthlyReviewsDueDto MonthlyReviews
);

/// <summary>
/// Overview of the day for coaches (their clients and appointments)
/// and administrators (all clients and appointments).
/// </summary>
public class GetTodayOverviewEndpoint : Endpoint<GetTodayOverviewRequest, GetTodayOverviewResponse>
{
    private const int DefaultInactiveDays = 14;

    // Low credits alert email is sent at one credit left.
    private const int LowCreditsLimit = 1;

    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTodayOverviewEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/dashboard/today");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Statistics"));
    }

    public override async Task HandleAsync(GetTodayOverviewRequest request, CancellationToken cancellationToken)
    {
        var inactiveDays = Math.Clamp(request.InactiveDays ?? DefaultInactiveDays, 1, 365);

        var timeZone = GetCroatianTimeZone();
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        var dayStart = new DateTimeOffset(now.Date, now.Offset);
        var dayEnd = dayStart.AddDays(1);

        var isAdministrator = TrainingAccess.IsAdministrator(_currentUserService);

        var coachId = isAdministrator
            ? (Guid?)null
            : await _context.Coaches
                .Where(x => x.UserId == _currentUserService.UserId)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

        if (!isAdministrator && coachId is null)
            ThrowError(ErrorCodes.NotFound);

        var clients = _context.Clients.WithActiveUserAccount();

        if (coachId is not null)
            clients = clients.Where(client => _context.CoachClients
                .Any(x => x.CoachId == coachId && x.ClientId == client.Id));

        var appointments = _context.Appointments.AsQueryable();

        if (coachId is not null)
            appointments = appointments.Where(x => x.Coaches.Any(coach => coach.CoachId == coachId));

        var todayAppointments = await appointments
            .AsNoTracking()
            .Where(x => x.StartTime >= dayStart && x.StartTime < dayEnd &&
                        (x.AppointmentStatus.Name == BusinessStatuses.Approved ||
                         x.AppointmentStatus.Name == BusinessStatuses.InProgress))
            .OrderBy(x => x.StartTime)
            .Select(x => new TodayAppointmentDto(x.Id, x.StartTime, x.EndTime, x.AppointmentStatus.Name,
                x.Clients.Select(client => client.Client.FirstName + " " + client.Client.LastName).ToList()))
            .ToListAsync(cancellationToken);

        var pendingAppointments = await appointments
            .CountAsync(x => x.AppointmentStatus.Name == BusinessStatuses.Pending && x.StartTime >= dayStart,
                cancellationToken);

        var pendingRequests = await _context.AppointmentRequests
            .Where(x => x.AppointmentRequestStatus.Name == BusinessStatuses.Pending)
            .Where(x => coachId == null || x.Appointment.Coaches.Any(coach => coach.CoachId == coachId))
            .GroupBy(x => x.AppointmentRequestType.Name)
            .Select(x => new { Type = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

        var lowCreditsClients = await clients
            .AsNoTracking()
            .Where(x => x.Credits <= LowCreditsLimit)
            .OrderBy(x => x.Credits)
            .ThenBy(x => x.FirstName)
            .Select(x => new LowCreditsClientDto(x.Id, x.FirstName + " " + x.LastName, x.Credits))
            .ToListAsync(cancellationToken);

        var inactiveClients = await GetInactiveClients(clients, now, inactiveDays, cancellationToken);

        var monthlyReviews = await GetMonthlyReviewsDue(clients, now, cancellationToken);

        await SendAsync(new GetTodayOverviewResponse(
                todayAppointments,
                new PendingRequestsDto(pendingAppointments,
                    pendingRequests.GetValueOrDefault(BusinessActions.CancelationRequest),
                    pendingRequests.GetValueOrDefault(BusinessActions.JoinRequest)),
                lowCreditsClients,
                inactiveDays,
                inactiveClients,
                monthlyReviews),
            cancellation: cancellationToken);
    }

    /// <summary>
    /// Clients whose last activity (completed training or approved
    /// appointment that already started) is older than the given days,
    /// clients that were never active come last.
    /// </summary>
    private async Task<List<InactiveClientDto>> GetInactiveClients(IQueryable<Client> clients,
        DateTimeOffset now, int inactiveDays, CancellationToken cancellationToken)
    {
        var since = now.AddDays(-inactiveDays);

        var activity = await clients
            .AsNoTracking()
            .Select(client => new
            {
                client.Id,
                FullName = client.FirstName + " " + client.LastName,
                LastTraining = _context.Trainings
                    .Where(x => x.ClientId == client.Id && x.IsCompleted && x.Date <= now)
                    .Max(x => (DateTimeOffset?)x.Date),
                LastAppointment = _context.Appointments
                    .Where(x => x.Clients.Any(appointmentClient => appointmentClient.ClientId == client.Id) &&
                                x.StartTime <= now &&
                                x.AppointmentStatus.Name == BusinessStatuses.Approved)
                    .Max(x => (DateTimeOffset?)x.StartTime)
            })
            .ToListAsync(cancellationToken);

        return activity
            .Select(x => new InactiveClientDto(x.Id, x.FullName,
                x.LastTraining > x.LastAppointment || x.LastAppointment is null ? x.LastTraining : x.LastAppointment))
            .Where(x => x.LastActivityAt is null || x.LastActivityAt < since)
            .OrderBy(x => x.LastActivityAt is null)
            .ThenBy(x => x.LastActivityAt)
            .ThenBy(x => x.FullName)
            .ToList();
    }

    /// <summary>
    /// Reviews of the current month are written before the 1st of the next one.
    /// </summary>
    private async Task<MonthlyReviewsDueDto> GetMonthlyReviewsDue(IQueryable<Client> clients,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        var reviewedClientIds = _context.MonthlyReviews
            .Where(x => x.Year == now.Year && x.Month == now.Month)
            .Select(x => x.ClientId);

        var missingClients = await clients
            .AsNoTracking()
            .Where(x => !reviewedClientIds.Contains(x.Id))
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Select(x => new OverviewClientDto(x.Id, x.FirstName + " " + x.LastName))
            .ToListAsync(cancellationToken);

        var firstOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);

        return new MonthlyReviewsDueDto(now.Year, now.Month, firstOfMonth.AddMonths(1), missingClients);
    }

    private static TimeZoneInfo GetCroatianTimeZone()
    {
        // IANA id on Linux and newer Windows, Windows id as fallback.
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Zagreb", out var timeZone))
            return timeZone;

        return TimeZoneInfo.TryFindSystemTimeZoneById("Central European Standard Time", out timeZone)
            ? timeZone
            : TimeZoneInfo.Utc;
    }
}
