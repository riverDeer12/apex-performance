using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// Requests nobody answered before the appointment started
/// can no longer be approved, so they are declined:
/// appointments clients asked for (Pending) and cancelation
/// or join requests (Pending or InProgress).
/// </summary>
public class DeclineExpiredRequestsJob
{
    // Every half hour from 5:00 to 21:00 Croatian time, not during the night.
    // Cron can not express 5:00-21:00 in one expression, so 21:00 is a second job.
    private const string DayJobId = "decline-expired-requests";
    private const string DaySchedule = "0,30 5-20 * * *";
    private const string LastRunJobId = "decline-expired-requests-21h";
    private const string LastRunSchedule = "0 21 * * *";

    private readonly ApexPerformanceContext _context;
    private readonly ILogger<DeclineExpiredRequestsJob> _logger;

    public DeclineExpiredRequestsJob(ApexPerformanceContext context, ILogger<DeclineExpiredRequestsJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public static void Schedule()
    {
        var options = new RecurringJobOptions { TimeZone = GetCroatianTimeZone() };

        RecurringJob.AddOrUpdate<DeclineExpiredRequestsJob>(DayJobId, job => job.Run(), DaySchedule, options);
        RecurringJob.AddOrUpdate<DeclineExpiredRequestsJob>(LastRunJobId, job => job.Run(), LastRunSchedule,
            options);
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

    public async Task Run()
    {
        var now = DateTimeOffset.UtcNow;

        var declinedAppointments = await DeclineExpiredAppointments(now);

        var declinedRequests = await DeclineExpiredAppointmentRequests(now);

        if (declinedAppointments + declinedRequests > 0)
            _logger.LogInformation(
                "Declined {Appointments} expired appointment requests and {Requests} expired cancelation/join requests.",
                declinedAppointments, declinedRequests);
    }

    private async Task<int> DeclineExpiredAppointments(DateTimeOffset now)
    {
        var declinedStatus = await _context.AppointmentStatuses
            .SingleAsync(x => x.Name == BusinessStatuses.Declined);

        var expiredAppointments = await _context.Appointments
            .Where(appointment =>
                appointment.AppointmentStatus.Name == BusinessStatuses.Pending &&
                appointment.StartTime <= now)
            .ToListAsync();

        foreach (var appointment in expiredAppointments)
            appointment.AppointmentStatus = declinedStatus;

        await _context.SaveChangesAsync();

        return expiredAppointments.Count;
    }

    private async Task<int> DeclineExpiredAppointmentRequests(DateTimeOffset now)
    {
        var declinedStatus = await _context.AppointmentRequestStatuses
            .SingleAsync(x => x.Name == BusinessStatuses.Declined);

        var expiredRequests = await _context.AppointmentRequests
            .Where(request =>
                (request.AppointmentRequestStatus.Name == BusinessStatuses.Pending ||
                 request.AppointmentRequestStatus.Name == BusinessStatuses.InProgress) &&
                request.Appointment.StartTime <= now)
            .ToListAsync();

        foreach (var request in expiredRequests)
            request.AppointmentRequestStatus = declinedStatus;

        await _context.SaveChangesAsync();

        return expiredRequests.Count;
    }
}
