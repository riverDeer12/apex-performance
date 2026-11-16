using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Utilities;
using FirebaseAdmin.Messaging;
using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// Two hours before an approved appointment starts, its clients get a push on
/// all their devices ("Trening za 2 h", "Danas u 18:00 · coach"). The clients
/// are read when it is sent, so clients who joined later get it too and those
/// who left don't. See docs/NOTIFICATIONS_API.md (Appointment reminder).
/// </summary>
public class AppointmentReminderJob
{
    private static readonly HashSet<MessagingErrorCode> InvalidTokenErrors =
    [
        MessagingErrorCode.Unregistered,
        MessagingErrorCode.SenderIdMismatch
    ];

    // States of a reminder job that will still run.
    private static readonly HashSet<string> PendingStates =
        [ScheduledState.StateName, EnqueuedState.StateName, ProcessingState.StateName];

    private readonly ApexPerformanceContext _context;
    private readonly ILogger<AppointmentReminderJob> _logger;

    public AppointmentReminderJob(ApexPerformanceContext context, ILogger<AppointmentReminderJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static string HashKey(Guid appointmentId) => $"appointment-reminder:{appointmentId}";

    /// <summary>
    /// Schedules the reminder of an appointment whose status or start may have
    /// changed (see <see cref="AppointmentReminder.ShouldSchedule"/>), once per
    /// appointment: the job of the appointment is kept in Hangfire (a hash with
    /// its id and start), so calling this again for the same start does
    /// nothing, and a new start replaces the earlier job. Cancelled, declined
    /// or deleted appointments need no call: the job checks when it runs.
    /// Never throws, the change that calls it is already saved.
    /// </summary>
    public static void Schedule(Guid appointmentId, string status, DateTimeOffset startTime, ILogger? logger = null)
    {
        var now = DateTimeOffset.UtcNow;

        if (!AppointmentReminder.ShouldSchedule(status, startTime, now))
            return;

        try
        {
            var key = HashKey(appointmentId);
            using var connection = JobStorage.Current.GetConnection();
            using var _ = connection.AcquireDistributedLock(key, TimeSpan.FromSeconds(10));

            var existing = connection.GetAllEntriesFromHash(key);
            var existingJobId = existing?.GetValueOrDefault("JobId");

            if (existingJobId is not null &&
                long.TryParse(existing!.GetValueOrDefault("StartTime"), out var ticks) &&
                AppointmentReminder.SameInstant(new DateTimeOffset(ticks, TimeSpan.Zero), startTime) &&
                connection.GetJobData(existingJobId) is { } job && PendingStates.Contains(job.State))
                return;

            var delay = AppointmentReminder.Delay(startTime, now);
            var jobId = BackgroundJob.Schedule<AppointmentReminderJob>(x => x.Run(appointmentId, startTime), delay);

            using (var transaction = connection.CreateWriteTransaction())
            {
                transaction.SetRangeInHash(key, new Dictionary<string, string>
                {
                    ["JobId"] = jobId,
                    ["StartTime"] = startTime.UtcTicks.ToString()
                });

                // Gone a day after the appointment.
                if (transaction is JobStorageTransaction storageTransaction)
                    storageTransaction.ExpireHash(key, startTime - now + TimeSpan.FromDays(1));

                transaction.Commit();
            }

            if (existingJobId is not null && existingJobId != jobId)
                BackgroundJob.Delete(existingJobId);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Reminder of appointment {AppointmentId} could not be scheduled.", appointmentId);
        }
    }

    /// <summary>
    /// Reminders of the approved appointments still ahead that have none, e.g.
    /// approved before reminders existed. Run at startup.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task ScheduleUpcoming()
    {
        var from = DateTimeOffset.UtcNow + AppointmentReminder.Before;

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.AppointmentStatus.Name == BusinessStatuses.Approved && x.StartTime >= from)
            .Select(x => new { x.Id, x.StartTime })
            .ToListAsync();

        foreach (var appointment in appointments)
            Schedule(appointment.Id, BusinessStatuses.Approved, appointment.StartTime, _logger);

        _logger.LogInformation("Reminders checked for {Count} upcoming approved appointments.", appointments.Count);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task Run(Guid appointmentId, DateTimeOffset startTime)
    {
        var appointment = await _context.Appointments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == appointmentId)
            .Select(x => new
            {
                x.Id,
                x.StartTime,
                Status = x.AppointmentStatus.Name,
                x.IsDeleted,
                Coaches = x.Coaches.Select(c => c.Coach.FirstName + " " + c.Coach.LastName).ToList(),
                ClientUserIds = x.Clients.Where(c => !c.Client.IsDeleted).Select(c => c.Client.UserId).ToList()
            })
            .FirstOrDefaultAsync();

        var reminder = appointment is null
            ? null
            : new AppointmentReminder.Appointment(appointment.Id, appointment.StartTime, appointment.Status,
                appointment.IsDeleted, appointment.Coaches);

        var now = DateTimeOffset.UtcNow;

        if (!AppointmentReminder.ShouldSend(reminder, startTime, now))
        {
            _logger.LogInformation(
                "No reminder for appointment {AppointmentId}: it isn't approved at that start any more or starts too soon.",
                appointmentId);
            return;
        }

        var userIds = appointment!.ClientUserIds.Distinct().ToList();

        var devices = await _context.DeviceTokens
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new AppointmentReminder.Device(x.Token, x.Platform, x.Language))
            .ToListAsync();

        devices = devices.GroupBy(x => x.Token).Select(x => x.First()).ToList();

        if (devices.Count == 0)
            return;

        var timeZone = AppointmentReminder.CroatianTimeZone();
        var messages = devices.Select(x => AppointmentReminder.MessageFor(reminder!, x, now, timeZone)).ToList();

        BatchResponse response;

        try
        {
            response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages);
        }
        catch (Exception ex) when (ex is FirebaseMessagingException or HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Reminder of appointment {AppointmentId} could not be sent ({Count} devices).",
                appointmentId, messages.Count);
            return;
        }

        var invalid = new List<string>();

        for (var i = 0; i < response.Responses.Count; i++)
        {
            var result = response.Responses[i];
            var device = devices[i];

            if (result.IsSuccess)
                continue;

            var code = result.Exception?.MessagingErrorCode;
            var error = result.Exception?.Message ?? "Unknown error";
            var isInvalid = code is { } c && InvalidTokenErrors.Contains(c) ||
                            code == MessagingErrorCode.InvalidArgument &&
                            error.Contains("registration token", StringComparison.OrdinalIgnoreCase);

            if (isInvalid)
                invalid.Add(device.Token);

            _logger.Log(isInvalid ? LogLevel.Warning : LogLevel.Error,
                "Reminder of appointment {AppointmentId} was not delivered to a {Platform} device: {ErrorCode} - {Error}",
                appointmentId, device.Platform, code?.ToString() ?? "Unknown", error);
        }

        _logger.LogInformation("Reminder of appointment {AppointmentId} sent to {Count} of {Total} devices.",
            appointmentId, response.SuccessCount, messages.Count);

        if (invalid.Count > 0)
        {
            // Like NotificationService: the token will never work again.
            await _context.DeviceTokens
                .Where(x => invalid.Contains(x.Token))
                .ExecuteDeleteAsync();
        }
    }
}
