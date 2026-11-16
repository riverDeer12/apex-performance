using System.Globalization;
using ApexPerformance.API.Constants;
using FirebaseAdmin.Messaging;

namespace ApexPerformance.API.Utilities;

/// <summary>
/// Reminder to the clients of an approved appointment two hours before it
/// starts ("Trening za 2 h" / "Danas u 18:00 · coach"). When to schedule and
/// send it and the Firebase messages, without the database, so it is unit
/// tested. See docs/NOTIFICATIONS_API.md (Appointment reminder).
/// </summary>
public static class AppointmentReminder
{
    public const string Type = PushNotificationTypes.AppointmentReminder;

    public static readonly TimeSpan Before = TimeSpan.FromHours(2);

    // A job that runs late (the server was down) still reminds while at least
    // this much is left; closer to the start the client is (or isn't) on the way.
    public static readonly TimeSpan MinimumLeadWhenSent = TimeSpan.FromMinutes(15);

    // Times in the texts are Croatian time, like the gym's schedule.
    public static TimeZoneInfo CroatianTimeZone()
    {
        // IANA id on Linux and newer Windows, Windows id as fallback.
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Zagreb", out var timeZone))
            return timeZone;

        return TimeZoneInfo.TryFindSystemTimeZoneById("Central European Standard Time", out timeZone)
            ? timeZone
            : TimeZoneInfo.Utc;
    }

    public record Appointment(
        Guid Id,
        DateTimeOffset StartTime,
        string Status,
        bool IsDeleted,
        IReadOnlyList<string> CoachNames);

    public record Device(string Token, string Platform, string? Language);

    /// <summary>
    /// Scheduled only for an approved appointment that is still at least two
    /// hours away. One approved (or created approved) with less than two hours
    /// left gets none, not even a late one: the client has just booked it or
    /// just been told it was approved.
    /// </summary>
    public static bool ShouldSchedule(string status, DateTimeOffset startTime, DateTimeOffset now)
        => status == BusinessStatuses.Approved && startTime - now >= Before;

    /// <summary>
    /// From now until two hours before the start (zero if that has passed).
    /// </summary>
    public static TimeSpan Delay(DateTimeOffset startTime, DateTimeOffset now)
    {
        var delay = startTime - Before - now;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }

    /// <summary>
    /// Sent only if the appointment is still approved, not deleted, at the
    /// start the reminder was scheduled for (a changed start has its own
    /// reminder) and still at least 15 minutes away.
    /// </summary>
    public static bool ShouldSend(Appointment? appointment, DateTimeOffset scheduledStartTime, DateTimeOffset now)
        => appointment is { IsDeleted: false, Status: BusinessStatuses.Approved } &&
           SameInstant(appointment.StartTime, scheduledStartTime) &&
           appointment.StartTime - now >= MinimumLeadWhenSent;

    // The database keeps less precision than the value the job was scheduled with.
    public static bool SameInstant(DateTimeOffset a, DateTimeOffset b)
        => Math.Abs((a - b).Ticks) < TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// "Trening za 2 h" / "Danas u 18:00 · Ivan Horvat" in the device's
    /// language, the time in Croatian time. "Sutra" when the start is after
    /// midnight of the day the reminder is sent.
    /// </summary>
    public static PushText Text(string? language, Appointment appointment, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var start = TimeZoneInfo.ConvertTime(appointment.StartTime, timeZone);
        var today = TimeZoneInfo.ConvertTime(now, timeZone).Date;
        var day = start.Date == today ? PushTexts.ReminderDay.Today
            : start.Date == today.AddDays(1) ? PushTexts.ReminderDay.Tomorrow
            : PushTexts.ReminderDay.Other;

        var coaches = string.Join(", ", appointment.CoachNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim()));

        return PushTexts.AppointmentReminder(PushTexts.Normalize(language) ?? PushTexts.Croatian, day,
            start.ToString("HH:mm", CultureInfo.InvariantCulture),
            start.ToString("d.M.", CultureInfo.InvariantCulture), coaches);
    }

    public static Dictionary<string, string> Data(Appointment appointment) => new()
    {
        ["type"] = Type,
        ["appointmentId"] = appointment.Id.ToString()
    };

    /// <summary>
    /// A normal notification like the other pushes, on Android too (the app
    /// shows notification pushes; data-only ones only the training end
    /// reminder). Of no use after the start, so it expires then.
    /// </summary>
    public static Message MessageFor(Appointment appointment, Device device, DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        var text = Text(device.Language, appointment, now, timeZone);
        var id = appointment.Id.ToString();
        var timeToLive = appointment.StartTime - now;

        if (timeToLive < TimeSpan.Zero)
            timeToLive = TimeSpan.Zero;

        return new Message
        {
            Token = device.Token,
            Notification = new Notification { Title = text.Title, Body = text.Body },
            Data = Data(appointment),
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                TimeToLive = timeToLive,
                // A later reminder of the same appointment replaces this one.
                Notification = new AndroidNotification { Tag = id }
            },
            Apns = new ApnsConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["apns-collapse-id"] = id,
                    ["apns-expiration"] = appointment.StartTime.ToUnixTimeSeconds().ToString()
                },
                Aps = new Aps { Sound = "default", ThreadId = id }
            }
        };
    }
}
