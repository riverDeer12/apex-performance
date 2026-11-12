using System.Globalization;
using ApexPerformance.API.Constants;
using FirebaseAdmin.Messaging;

namespace ApexPerformance.API.Utilities;

/// <summary>
/// Reminder to the coach that a training they started is still running an
/// hour later, with a button that ends it ("End training"). Nothing ends by
/// itself: only the coach's tap does. When to send it and the Firebase
/// messages, without the database, so it is unit tested.
/// See docs/LIVE_ACTIVITY_PUSH.md (Training end reminder).
/// </summary>
public static class TrainingEndReminder
{
    public const string Type = PushNotificationTypes.TrainingEndReminder;

    // UNNotificationCategory of the iOS app with the "End training" action (END_TRAINING).
    public const string IosCategory = "TRAINING_END_REMINDER";

    public static readonly TimeSpan After = TimeSpan.FromHours(1);

    // The longest a training can last (ChangeTrainingTimesValidator): after
    // that the button couldn't end it any more, so a late job sends nothing.
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(6);

    public record Training(
        Guid Id,
        string Name,
        string ClientName,
        DateTimeOffset? StartedAt,
        DateTimeOffset? EndedAt,
        bool IsCompleted,
        bool IsDeleted)
    {
        public bool IsRunning => !IsDeleted && StartedAt is not null && EndedAt is null && !IsCompleted;
    }

    public record Device(string Token, string Platform, string? Language);

    /// <summary>
    /// A reminder is scheduled for each new start: the training runs after the
    /// change and its start time is not the one it had. Ended, cleared or
    /// unchanged starts schedule nothing (a training reopened with the same
    /// start keeps the reminder scheduled at its start). Neither does a start
    /// set more than an hour back: the coach is changing the times right now.
    /// </summary>
    public static bool ShouldSchedule(DateTimeOffset? previousStartedAt, DateTimeOffset? startedAt,
        DateTimeOffset? endedAt, bool isCompleted, DateTimeOffset now)
        => startedAt is not null && endedAt is null && !isCompleted &&
           !SameInstant(previousStartedAt, startedAt) &&
           startedAt.Value + After > now;

    /// <summary>
    /// From now until an hour after the start (zero if that has passed).
    /// </summary>
    public static TimeSpan Delay(DateTimeOffset startedAt, DateTimeOffset now)
    {
        var delay = startedAt + After - now;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }

    /// <summary>
    /// Sent only if the training still runs with the start the reminder was
    /// scheduled for: a changed start has its own reminder, an ended, completed
    /// or deleted training none.
    /// </summary>
    public static bool ShouldSend(Training? training, DateTimeOffset scheduledStartedAt, DateTimeOffset now)
        => training is { IsRunning: true } &&
           SameInstant(training.StartedAt, scheduledStartedAt) &&
           now - scheduledStartedAt <= MaxDuration;

    // The database keeps less precision than the value the job was scheduled with.
    private static bool SameInstant(DateTimeOffset? a, DateTimeOffset? b)
        => a is null || b is null
            ? a is null && b is null
            : Math.Abs((a.Value - b.Value).Ticks) < TimeSpan.TicksPerMillisecond;

    public static PushText Text(string? language, string trainingName, string clientName)
        => PushTexts.TrainingEndReminder(PushTexts.Normalize(language) ?? PushTexts.Croatian,
            string.IsNullOrWhiteSpace(clientName) ? trainingName : $"{trainingName} · {clientName.Trim()}");

    /// <summary>
    /// "type", "trainingId" and "startedAt" (ISO 8601 UTC with milliseconds),
    /// which the apps send back with the end time.
    /// </summary>
    public static Dictionary<string, string> Data(Training training) => new()
    {
        ["type"] = Type,
        ["trainingId"] = training.Id.ToString(),
        ["startedAt"] = training.StartedAt!.Value.UtcDateTime
            .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// iOS: a notification of the category with the "End training" action.
    /// Android: data only, so the app always shows it itself, with the action
    /// (a notification payload would be shown by the system without it).
    /// </summary>
    public static Message MessageFor(Training training, Device device)
    {
        var text = Text(device.Language, training.Name, training.ClientName);
        var data = Data(training);

        if (string.Equals(device.Platform, "android", StringComparison.OrdinalIgnoreCase))
        {
            data["title"] = text.Title;
            data["body"] = text.Body;

            return new Message
            {
                Token = device.Token,
                Data = data,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    // Of no use once the training would have to be ended anyway.
                    TimeToLive = MaxDuration - After
                }
            };
        }

        return new Message
        {
            Token = device.Token,
            Notification = new Notification { Title = text.Title, Body = text.Body },
            Data = data,
            Apns = new ApnsConfig
            {
                // A later reminder of the same training replaces this one.
                Headers = new Dictionary<string, string> { ["apns-collapse-id"] = training.Id.ToString() },
                Aps = new Aps
                {
                    Sound = "default",
                    Category = IosCategory,
                    ThreadId = training.Id.ToString()
                }
            }
        };
    }
}
