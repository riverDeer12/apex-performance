using ApexPerformance.API.Constants;
using FirebaseAdmin.Messaging;

namespace ApexPerformance.API.Utilities;

/// <summary>
/// Which iOS devices get which Live Activity push for a training, and the
/// Firebase messages themselves (FCM HTTP v1 with apns.live_activity_token).
/// No database here, so it is unit tested. See docs/LIVE_ACTIVITY_PUSH.md.
/// </summary>
public static class LiveActivityPush
{
    // Name of the Swift ActivityAttributes type the app's widget renders.
    public const string AttributesType = "TrainingActivityAttributes";

    public const string Start = "start";
    public const string Update = "update";
    public const string End = "end";

    // An ended training stays on the lock screen with its duration, like on
    // the iPhone that ends it (TrainingLiveActivity.end in the app).
    public static readonly TimeSpan EndedVisibleFor = TimeSpan.FromMinutes(15);

    // Swift's Date is encoded by JSONEncoder's default strategy, which
    // ActivityKit also uses to decode content-state: seconds since this date.
    private static readonly DateTimeOffset ReferenceDate = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Training as the Live Activity shows it.
    /// </summary>
    /// <param name="PreviousStartedAt">Start before the change, for the final state of cleared times.</param>
    /// <param name="ClientName">Shown on the staff's devices.</param>
    /// <param name="CoachName">Shown on the client's devices instead of their own name, null when not known.</param>
    public record Training(
        Guid Id,
        string Name,
        DateTimeOffset? StartedAt,
        DateTimeOffset? EndedAt,
        bool IsCompleted,
        bool IsDeleted,
        DateTimeOffset? PreviousStartedAt,
        Guid ClientUserId,
        string ClientName,
        string? CoachName)
    {
        // Same as the app: started, not ended and not completed.
        public bool IsRunning => !IsDeleted && StartedAt is not null && EndedAt is null && !IsCompleted;

        // Ended normally: the activity shows the duration for a while.
        public bool HasEnded => !IsDeleted && StartedAt is not null && EndedAt is not null;
    }

    /// <summary>
    /// iOS device (FCM registration token) with its Live Activity tokens:
    /// push-to-start of the device and update token of this training.
    /// </summary>
    public record Device(
        Guid DeviceTokenId,
        string FcmToken,
        Guid UserId,
        string? Language,
        string? PushToStartToken,
        string? UpdateToken);

    /// <summary>
    /// One push: to the FCM token of the device, with the activity token
    /// (push-to-start for "start", the activity's own for "update" and "end").
    /// </summary>
    public record Send(Guid DeviceTokenId, string Event, string ActivityToken, Message Message);

    /// <summary>
    /// The pushes after a training changed.
    /// <list type="bullet">
    /// <item>Running and it was not before: "start" on the client's devices and on the
    /// other devices of the staff user who started it, unless they already show it
    /// (have an update token) or are the device that made the change (shows it itself).</item>
    /// <item>Running: "update" on every device that shows it.</item>
    /// <item>Not running: "end" on every device that shows it.</item>
    /// </list>
    /// </summary>
    public static List<Send> Plan(Training training, IEnumerable<Device> devices, bool wasRunning,
        Guid? actorUserId, string? excludeFcmToken, DateTimeOffset now)
    {
        var sends = new List<Send>();
        // A device can be listed twice (e.g. the client is also the actor); once is enough.
        var all = devices.GroupBy(x => x.DeviceTokenId).Select(x => x.First()).ToList();

        foreach (var device in all)
        {
            if (device.UpdateToken is not null)
            {
                var message = training.IsRunning
                    ? UpdateMessage(training, device.FcmToken, device.UpdateToken, now)
                    : EndMessage(training, device.FcmToken, device.UpdateToken, now);

                sends.Add(new Send(device.DeviceTokenId, training.IsRunning ? Update : End, device.UpdateToken,
                    message));
                continue;
            }

            if (!training.IsRunning || wasRunning || device.PushToStartToken is null)
                continue;

            var isClient = device.UserId == training.ClientUserId;
            var isActor = actorUserId is not null && device.UserId == actorUserId;

            if (!isClient && !isActor)
                continue;

            if (isActor && !isClient && excludeFcmToken is not null && device.FcmToken == excludeFcmToken)
                continue;

            sends.Add(new Send(device.DeviceTokenId, Start, device.PushToStartToken,
                StartMessage(training, device.FcmToken, device.PushToStartToken, device.Language, isClient, now)));
        }

        return sends;
    }

    public static Message StartMessage(Training training, string fcmToken, string pushToStartToken,
        string? language, bool forClient, DateTimeOffset now)
    {
        var texts = PushTexts.LiveActivity(language ?? PushTexts.Croatian);

        // The client sees who trains them, the staff whom they train.
        var name = forClient ? training.CoachName ?? "" : training.ClientName;

        var attributes = new Dictionary<string, object>
        {
            // The app's activities of saved trainings use the training id as key.
            ["key"] = IdOf(training.Id),
            ["clientName"] = name,
            ["inProgressText"] = texts.InProgress,
            ["endedText"] = texts.Ended,
            ["endText"] = texts.End,
            // Clients can't end trainings.
            ["showsEndButton"] = !forClient
        };

        var aps = new Dictionary<string, object>
        {
            ["timestamp"] = now.ToUnixTimeSeconds(),
            ["event"] = Start,
            ["content-state"] = ContentState(training),
            ["attributes-type"] = AttributesType,
            ["attributes"] = attributes
        };

        // Apple needs an alert to start an activity; it lights up the screen without a sound.
        var alert = new ApsAlert
        {
            Title = texts.InProgress,
            Body = string.IsNullOrWhiteSpace(name) ? training.Name : $"{training.Name} · {name}"
        };

        return Build(fcmToken, pushToStartToken, aps, alert);
    }

    public static Message UpdateMessage(Training training, string fcmToken, string updateToken,
        DateTimeOffset now)
    {
        var aps = new Dictionary<string, object>
        {
            ["timestamp"] = now.ToUnixTimeSeconds(),
            ["event"] = Update,
            ["content-state"] = ContentState(training)
        };

        return Build(fcmToken, updateToken, aps, null);
    }

    /// <summary>
    /// Ended training: the final state with its duration, dismissed after
    /// <see cref="EndedVisibleFor"/>; otherwise (times cleared, completed without
    /// times, deleted) the activity goes away right away.
    /// </summary>
    public static Message EndMessage(Training training, string fcmToken, string updateToken, DateTimeOffset now)
    {
        var dismissal = training.HasEnded ? training.EndedAt!.Value + EndedVisibleFor : now;

        var aps = new Dictionary<string, object>
        {
            ["timestamp"] = now.ToUnixTimeSeconds(),
            ["event"] = End,
            ["content-state"] = ContentState(training, final: true),
            ["dismissal-date"] = dismissal.ToUnixTimeSeconds()
        };

        return Build(fcmToken, updateToken, aps, null);
    }

    /// <summary>
    /// TrainingActivityAttributes.ContentState of the app: dates in seconds
    /// since 2001-01-01 (Swift's default Date coding), endedAt left out while
    /// running, trainingId as Swift's uuidString (upper case).
    /// </summary>
    public static Dictionary<string, object> ContentState(Training training, bool final = false)
    {
        var startedAt = training.StartedAt ?? training.PreviousStartedAt ?? training.EndedAt ?? DateTimeOffset.UnixEpoch;

        var state = new Dictionary<string, object>
        {
            ["startedAt"] = SwiftDate(startedAt),
            ["trainingId"] = IdOf(training.Id),
            ["trainingName"] = training.Name
        };

        if (final && training.HasEnded)
            state["endedAt"] = SwiftDate(training.EndedAt!.Value);

        return state;
    }

    /// <summary>
    /// Seconds since 2001-01-01 UTC (Date.timeIntervalSinceReferenceDate), to the millisecond.
    /// </summary>
    public static double SwiftDate(DateTimeOffset date)
        => Math.Round((date - ReferenceDate).TotalSeconds, 3);

    // Swift's UUID.uuidString, which the app compares training ids with.
    public static string IdOf(Guid id) => id.ToString("D").ToUpperInvariant();

    private static Message Build(string fcmToken, string activityToken, Dictionary<string, object> aps,
        ApsAlert? alert)
        => new()
        {
            Token = fcmToken,
            Apns = new ApnsConfig
            {
                LiveActivityToken = activityToken,
                Headers = new Dictionary<string, string> { ["apns-priority"] = "10" },
                Aps = new Aps
                {
                    Alert = alert,
                    CustomData = aps
                }
            }
        };
}
