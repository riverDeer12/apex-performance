using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services.Implementation;

public class NotificationService : INotificationService
{
    // Errors about the device token itself: the app was uninstalled, the token expired
    // or belongs to another Firebase project. Normal over time, so they are warnings
    // (no alert email) and the token is removed, it will never work again.
    private static readonly HashSet<MessagingErrorCode> InvalidTokenErrors =
    [
        MessagingErrorCode.Unregistered,
        MessagingErrorCode.SenderIdMismatch
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IServiceScopeFactory scopeFactory, ILogger<NotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<string> SendToDevice(string deviceToken, string title, string body,
        Dictionary<string, string>? data = null)
    {
        var message = new Message()
        {
            Token = deviceToken,
            Notification = new Notification()
            {
                Title = title,
                Body = body,
            },
            Data = data ?? new Dictionary<string, string>(),

            // iOS-specific config
            Apns = new ApnsConfig()
            {
                Aps = new Aps()
                {
                    Sound = "default",
                    Badge = 1,
                }
            }
        };

        try
        {
            return await FirebaseMessaging.DefaultInstance.SendAsync(message); // Message ID on success
        }
        catch (FirebaseMessagingException ex)
        {
            await HandleFailures([(deviceToken, ex.MessagingErrorCode, ex.Message)], title, data);
            throw;
        }
    }

    public async Task<BatchResponse?> SendToMultipleDevices(List<string> deviceTokens, string title, string body,
        Dictionary<string, string>? data = null)
    {
        // Firebase throws on an empty token list, which would abort the calling job
        // and skip any notifications it sends afterwards.
        if (deviceTokens.Count is 0)
            return null;

        var message = new MulticastMessage()
        {
            Tokens = deviceTokens,
            Notification = new Notification()
            {
                Title = title,
                Body = body,
            },
            Data = data ?? new Dictionary<string, string>(),
            Apns = new ApnsConfig()
            {
                Aps = new Aps() { Sound = "default" }
            }
        };

        BatchResponse response;

        try
        {
            response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
        }
        catch (Exception ex) when (ex is FirebaseMessagingException or HttpRequestException or TaskCanceledException)
        {
            // Nothing was sent (Firebase unreachable, wrong credentials...). The action that
            // sends the notification already happened, so it is logged and not thrown.
            _logger.LogError(ex, "Push notification {Title} ({NotificationType}) could not be sent to {Count} devices.",
                title, NotificationType(data), deviceTokens.Count);
            return null;
        }

        if (response.FailureCount > 0)
        {
            var failures = response.Responses
                .Select((result, index) => (Result: result, Token: deviceTokens[index]))
                .Where(x => !x.Result.IsSuccess)
                .Select(x => (x.Token, x.Result.Exception?.MessagingErrorCode, x.Result.Exception?.Message ?? "Unknown error"))
                .ToList();

            await HandleFailures(failures, title, data);
        }

        return response;
    }

    /// <summary>
    /// One log entry per device that didn't get the notification, with the user it
    /// belongs to. Tokens that are no longer valid are removed.
    /// </summary>
    private async Task HandleFailures(IReadOnlyCollection<(string Token, MessagingErrorCode? Code, string Error)> failures,
        string title, Dictionary<string, string>? data)
    {
        Dictionary<string, (Guid UserId, string? UserName, string Platform)> devices = new();

        var invalidTokens = failures
            .Where(x => IsInvalidToken(x.Code, x.Error))
            .Select(x => x.Token)
            .Distinct()
            .ToList();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApexPerformanceContext>();

            var tokens = failures.Select(x => x.Token).Distinct().ToList();

            devices = (await context.DeviceTokens
                    .AsNoTracking()
                    .Where(x => tokens.Contains(x.Token))
                    .Select(x => new { x.Token, x.UserId, UserName = x.User!.UserName, x.Platform })
                    .ToListAsync())
                .GroupBy(x => x.Token)
                .ToDictionary(x => x.Key, x => (x.First().UserId, x.First().UserName, x.First().Platform));

            if (invalidTokens.Count > 0)
            {
                // Deleted, not soft deleted: the same token registered again
                // (it never is once invalid) would otherwise be a duplicate.
                var removed = await context.DeviceTokens
                    .Where(x => invalidTokens.Contains(x.Token))
                    .ExecuteDeleteAsync();

                _logger.LogInformation("Removed {Count} device tokens that are no longer valid.", removed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Devices of failed push notifications could not be loaded or cleaned up.");
        }

        foreach (var (token, code, error) in failures)
        {
            var device = devices.GetValueOrDefault(token);
            var level = invalidTokens.Contains(token) ? LogLevel.Warning : LogLevel.Error;

            _logger.Log(level,
                "Push notification {Title} ({NotificationType}) was not delivered to {UserName} ({Platform}): {ErrorCode} - {Error} (UserId {UserId}, token {TokenStart}...)",
                title, NotificationType(data), device.UserName ?? "unknown user", device.Platform ?? "unknown device",
                code?.ToString() ?? "Unknown", error, device.UserId, token.Length > 12 ? token[..12] : token);
        }
    }

    // InvalidArgument is also returned for a wrong message, then all tokens would be
    // removed, so it counts only when Firebase says the token itself is not valid.
    private static bool IsInvalidToken(MessagingErrorCode? code, string error)
        => code is { } errorCode && InvalidTokenErrors.Contains(errorCode) ||
           code == MessagingErrorCode.InvalidArgument &&
           error.Contains("registration token", StringComparison.OrdinalIgnoreCase);

    private static string NotificationType(Dictionary<string, string>? data)
        => data?.GetValueOrDefault("type") ?? "none";

    public async Task SendLocalized(IReadOnlyCollection<PushRecipient> recipients, Func<string, PushText> text,
        Dictionary<string, string>? data = null)
    {
        // One message per language, devices without a language get Croatian.
        foreach (var group in recipients.GroupBy(x => PushTexts.Normalize(x.Language) ?? PushTexts.Croatian))
        {
            var localized = text(group.Key);
            await SendToMultipleDevices(group.Select(x => x.Token).Distinct().ToList(), localized.Title,
                localized.Body, data);
        }
    }

    public async Task<string> SendToTopic(string topic, string title, string body)
    {
        var message = new Message()
        {
            Topic = topic,
            Notification = new Notification()
            {
                Title = title,
                Body = body,
            }
        };

        return await FirebaseMessaging.DefaultInstance.SendAsync(message);
    }
}