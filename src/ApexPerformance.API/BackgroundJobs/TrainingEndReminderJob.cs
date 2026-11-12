using ApexPerformance.API.Database;
using ApexPerformance.API.Utilities;
using FirebaseAdmin.Messaging;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// An hour after a training was started: if it still runs with that start,
/// the staff user who started it gets a push on all their devices asking
/// whether to end it ("End training" ends it without opening the app).
/// The training is never ended here.
/// </summary>
public class TrainingEndReminderJob
{
    private static readonly HashSet<MessagingErrorCode> InvalidTokenErrors =
    [
        MessagingErrorCode.Unregistered,
        MessagingErrorCode.SenderIdMismatch
    ];

    private readonly ApexPerformanceContext _context;
    private readonly ILogger<TrainingEndReminderJob> _logger;

    public TrainingEndReminderJob(ApexPerformanceContext context, ILogger<TrainingEndReminderJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Schedules the reminder of a start (see <see cref="TrainingEndReminder.ShouldSchedule"/>).
    /// A changed start schedules a new one; the earlier one then finds another start and sends nothing.
    /// </summary>
    public static void Schedule(Guid trainingId, DateTimeOffset startedAt, Guid coachUserId)
        => BackgroundJob.Schedule<TrainingEndReminderJob>(job => job.Run(trainingId, startedAt, coachUserId),
            TrainingEndReminder.Delay(startedAt, DateTimeOffset.UtcNow));

    [AutomaticRetry(Attempts = 0)]
    public async Task Run(Guid trainingId, DateTimeOffset startedAt, Guid coachUserId)
    {
        var training = await _context.Trainings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == trainingId)
            .Select(x => new TrainingEndReminder.Training(x.Id, x.Name,
                x.Client.FirstName + " " + x.Client.LastName, x.StartedAt, x.EndedAt, x.IsCompleted, x.IsDeleted))
            .FirstOrDefaultAsync();

        if (!TrainingEndReminder.ShouldSend(training, startedAt, DateTimeOffset.UtcNow))
        {
            _logger.LogInformation("No end reminder for training {TrainingId}: it isn't running with that start any more.",
                trainingId);
            return;
        }

        var devices = await _context.DeviceTokens
            .AsNoTracking()
            .Where(x => x.UserId == coachUserId)
            .Select(x => new TrainingEndReminder.Device(x.Token, x.Platform, x.Language))
            .ToListAsync();

        devices = devices.GroupBy(x => x.Token).Select(x => x.First()).ToList();

        if (devices.Count == 0)
            return;

        var messages = devices.Select(x => TrainingEndReminder.MessageFor(training!, x)).ToList();

        BatchResponse response;

        try
        {
            response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages);
        }
        catch (Exception ex) when (ex is FirebaseMessagingException or HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "End reminder of training {TrainingId} could not be sent ({Count} devices).",
                trainingId, messages.Count);
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
                "End reminder of training {TrainingId} was not delivered to a {Platform} device of user {UserId}: {ErrorCode} - {Error}",
                trainingId, device.Platform, coachUserId, code?.ToString() ?? "Unknown", error);
        }

        _logger.LogInformation("End reminder of training {TrainingId} sent to {Count} of {Total} devices of user {UserId}.",
            trainingId, response.SuccessCount, messages.Count, coachUserId);

        if (invalid.Count > 0)
        {
            // Like NotificationService: the token will never work again.
            await _context.DeviceTokens
                .Where(x => invalid.Contains(x.Token))
                .ExecuteDeleteAsync();
        }
    }
}
