using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Utilities;
using FirebaseAdmin.Messaging;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// Starts, updates and ends the Live Activity of a training on iPhones
/// (the client's and the coach's other devices) after the training was
/// started, ended, changed or deleted. See docs/LIVE_ACTIVITY_PUSH.md.
/// </summary>
public class LiveActivityPushJob
{
    private static readonly HashSet<MessagingErrorCode> InvalidTokenErrors =
    [
        MessagingErrorCode.Unregistered,
        MessagingErrorCode.SenderIdMismatch
    ];

    private readonly ApexPerformanceContext _context;
    private readonly ILogger<LiveActivityPushJob> _logger;

    public LiveActivityPushJob(ApexPerformanceContext context, ILogger<LiveActivityPushJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Started, not ended and not completed: shown on the lock screens.
    /// </summary>
    public static bool IsRunning(Training training)
        => !training.IsDeleted && training.StartedAt is not null && training.EndedAt is null && !training.IsCompleted;

    /// <param name="wasRunning">Whether the training was running before the change; a start is sent only when it starts.</param>
    /// <param name="previousStartedAt">Start before the change, for the final state when times were cleared.</param>
    /// <param name="actorUserId">Staff user who made the change; their other iPhones show the training too.</param>
    /// <param name="actorDeviceToken">FCM token of the iPhone that made the change: it shows the training itself.</param>
    public static void Enqueue(Guid trainingId, bool wasRunning, DateTimeOffset? previousStartedAt = null,
        Guid? actorUserId = null, string? actorDeviceToken = null)
        => BackgroundJob.Enqueue<LiveActivityPushJob>(job =>
            job.Run(trainingId, wasRunning, previousStartedAt, actorUserId, actorDeviceToken));

    [AutomaticRetry(Attempts = 0)]
    public async Task Run(Guid trainingId, bool wasRunning, DateTimeOffset? previousStartedAt, Guid? actorUserId,
        string? actorDeviceToken)
    {
        // Deleted trainings too: their activities end.
        var training = await _context.Trainings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == trainingId)
            .Select(x => new
            {
                x.Id, x.Name, x.StartedAt, x.EndedAt, x.IsCompleted, x.IsDeleted,
                ClientUserId = x.Client.UserId,
                ClientName = x.Client.FirstName + " " + x.Client.LastName
            })
            .FirstOrDefaultAsync();

        if (training is null)
            return;

        var updateTokens = await _context.LiveActivityTokens
            .AsNoTracking()
            .Where(x => x.Kind == LiveActivityTokenKind.Update && x.TrainingId == trainingId)
            .Select(x => new { x.DeviceTokenId, x.Token, x.UpdatedAt })
            .ToListAsync();

        var startsNow = !wasRunning && training.StartedAt is not null && training.EndedAt is null &&
                        !training.IsCompleted && !training.IsDeleted;

        if (updateTokens.Count == 0 && !startsNow)
            return;

        var userIds = new List<Guid> { training.ClientUserId };
        if (actorUserId is not null) userIds.Add(actorUserId.Value);

        var updateDeviceIds = updateTokens.Select(x => x.DeviceTokenId).Distinct().ToList();

        var devices = await _context.DeviceTokens
            .AsNoTracking()
            .Where(x => updateDeviceIds.Contains(x.Id) || (startsNow && userIds.Contains(x.UserId) &&
                                                           x.Platform == "ios"))
            .Select(x => new { x.Id, x.Token, x.UserId, x.Language })
            .ToListAsync();

        var deviceIds = devices.Select(x => x.Id).ToList();

        var pushToStartTokens = startsNow
            ? (await _context.LiveActivityTokens
                .AsNoTracking()
                .Where(x => x.Kind == LiveActivityTokenKind.PushToStart && deviceIds.Contains(x.DeviceTokenId))
                .Select(x => new { x.DeviceTokenId, x.Token, x.UpdatedAt })
                .ToListAsync())
            .GroupBy(x => x.DeviceTokenId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(t => t.UpdatedAt).First().Token)
            : new Dictionary<Guid, string>();

        var latestUpdateTokens = updateTokens
            .GroupBy(x => x.DeviceTokenId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(t => t.UpdatedAt).First().Token);

        string? coachName = null;
        if (startsNow && actorUserId is not null)
        {
            coachName = await _context.Coaches.AsNoTracking()
                            .Where(x => x.UserId == actorUserId)
                            .Select(x => x.FirstName + " " + x.LastName)
                            .FirstOrDefaultAsync() ??
                        await _context.Administrators.AsNoTracking()
                            .Where(x => x.UserId == actorUserId)
                            .Select(x => x.FirstName + " " + x.LastName)
                            .FirstOrDefaultAsync();
        }

        var model = new LiveActivityPush.Training(training.Id, training.Name, training.StartedAt, training.EndedAt,
            training.IsCompleted, training.IsDeleted, previousStartedAt, training.ClientUserId,
            training.ClientName.Trim(), coachName?.Trim());

        var sends = LiveActivityPush.Plan(model,
            devices.Select(x => new LiveActivityPush.Device(x.Id, x.Token, x.UserId, x.Language,
                pushToStartTokens.GetValueOrDefault(x.Id), latestUpdateTokens.GetValueOrDefault(x.Id))),
            wasRunning, actorUserId, actorDeviceToken, DateTimeOffset.UtcNow);

        if (sends.Count > 0)
            await SendAll(trainingId, sends);

        // The activities ended: their tokens won't be used again. A new start
        // (times cleared and started again) starts new activities.
        if (!model.IsRunning && updateTokens.Count > 0)
        {
            await _context.LiveActivityTokens
                .Where(x => x.Kind == LiveActivityTokenKind.Update && x.TrainingId == trainingId)
                .ExecuteDeleteAsync();
        }
    }

    private async Task SendAll(Guid trainingId, List<LiveActivityPush.Send> sends)
    {
        BatchResponse response;

        try
        {
            response = await FirebaseMessaging.DefaultInstance.SendEachAsync(sends.Select(x => x.Message).ToList());
        }
        catch (Exception ex) when (ex is FirebaseMessagingException or HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Live Activity pushes of training {TrainingId} could not be sent ({Count}).",
                trainingId, sends.Count);
            return;
        }

        var invalid = new List<string>();

        for (var i = 0; i < response.Responses.Count; i++)
        {
            var result = response.Responses[i];
            var send = sends[i];

            if (result.IsSuccess)
            {
                _logger.LogInformation("Live Activity {Event} of training {TrainingId} sent to device {DeviceTokenId}.",
                    send.Event, trainingId, send.DeviceTokenId);
                continue;
            }

            var code = result.Exception?.MessagingErrorCode;
            var error = result.Exception?.Message ?? "Unknown error";
            var isInvalid = code is { } c && InvalidTokenErrors.Contains(c) ||
                            code == MessagingErrorCode.InvalidArgument &&
                            error.Contains("token", StringComparison.OrdinalIgnoreCase);

            if (isInvalid)
                invalid.Add(send.ActivityToken);

            _logger.Log(isInvalid ? LogLevel.Warning : LogLevel.Error,
                "Live Activity {Event} of training {TrainingId} was not delivered to device {DeviceTokenId}: {ErrorCode} - {Error}",
                send.Event, trainingId, send.DeviceTokenId, code?.ToString() ?? "Unknown", error);
        }

        if (invalid.Count > 0)
        {
            var removed = await _context.LiveActivityTokens
                .Where(x => invalid.Contains(x.Token))
                .ExecuteDeleteAsync();

            _logger.LogInformation("Removed {Count} Live Activity tokens that are no longer valid.", removed);
        }
    }
}
