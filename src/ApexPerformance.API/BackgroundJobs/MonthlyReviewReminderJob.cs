using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Clients;
using ApexPerformance.API.Services.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// Reminds coaches to write the monthly review of their clients
/// before the 1st, so clients can read it when the month ends.
/// Coaches are reminded on the 25th and the 28th about clients
/// that still have no review for the current month.
/// </summary>
public class MonthlyReviewReminderJob
{
    private const string JobId = "monthly-review-reminder";
    // 9:00 Croatian time on the 25th and 28th.
    private const string Schedule = "0 9 25,28 * *";

    private readonly ApexPerformanceContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MonthlyReviewReminderJob> _logger;

    public MonthlyReviewReminderJob(ApexPerformanceContext context, INotificationService notificationService,
        ILogger<MonthlyReviewReminderJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public static void ScheduleJob()
    {
        var options = new RecurringJobOptions { TimeZone = GetCroatianTimeZone() };

        RecurringJob.AddOrUpdate<MonthlyReviewReminderJob>(JobId, job => job.Run(), Schedule, options);
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

    [AutomaticRetry(Attempts = 0)]
    public async Task Run()
    {
        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, GetCroatianTimeZone());
        var year = today.Year;
        var month = today.Month;

        // Clients with an active account that have no review for this month yet.
        var reviewedClientIds = _context.MonthlyReviews
            .Where(x => x.Year == year && x.Month == month)
            .Select(x => x.ClientId);

        var missing = await _context.CoachClients
            .Where(x => !x.Coach.IsDeleted && x.Coach.User != null && !x.Coach.User.IsDeleted)
            .Where(x => _context.Clients.WithActiveUserAccount().Any(client => client.Id == x.ClientId))
            .Where(x => !reviewedClientIds.Contains(x.ClientId))
            .GroupBy(x => x.Coach.UserId)
            .Select(x => new { CoachUserId = x.Key, Count = x.Count() })
            .ToListAsync();

        foreach (var coach in missing)
        {
            var deviceTokens = await _context.DeviceTokens
                .Where(x => x.UserId == coach.CoachUserId)
                .Select(x => x.Token)
                .ToListAsync();

            _ = await _notificationService.SendToMultipleDevices(deviceTokens,
                "Monthly reviews",
                $"{coach.Count} client(s) still have no monthly review. Write it before the 1st.",
                PushNotificationTypes.Data(PushNotificationTypes.MonthlyReviewReminder));
        }

        _logger.LogInformation("Reminded {Coaches} coaches about missing monthly reviews for {Month}/{Year}.",
            missing.Count, month, year);
    }
}
