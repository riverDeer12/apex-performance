using ApexPerformance.API.Constants;
using ApexPerformance.API.Utilities;

namespace ApexPerformance.Tests;

public class AppointmentReminderTests
{
    private static readonly Guid AppointmentId = Guid.Parse("7d3f1c2a-4b5e-4f60-8a91-b2c3d4e5f607");

    // 18:00 in Zagreb (winter, UTC+1).
    private static readonly DateTimeOffset Start = new(2026, 11, 20, 17, 0, 0, 123, TimeSpan.Zero);
    private static readonly TimeZoneInfo Zagreb = AppointmentReminder.CroatianTimeZone();

    private static AppointmentReminder.Appointment Appointment(DateTimeOffset? start = null,
        string status = BusinessStatuses.Approved, bool isDeleted = false, params string[] coaches)
        => new(AppointmentId, start ?? Start, status, isDeleted, coaches.Length == 0 ? ["Ivan Horvat"] : coaches);

    [Fact]
    public void Scheduled_for_an_approved_appointment_at_least_two_hours_away()
    {
        Assert.True(AppointmentReminder.ShouldSchedule(BusinessStatuses.Approved, Start, Start.AddDays(-3)));
        Assert.True(AppointmentReminder.ShouldSchedule(BusinessStatuses.Approved, Start, Start.AddHours(-2)));
    }

    [Fact]
    public void Not_scheduled_when_not_approved_or_too_close()
    {
        foreach (var status in new[]
                 {
                     BusinessStatuses.Pending, BusinessStatuses.Declined, BusinessStatuses.Canceled,
                     BusinessStatuses.InProgress
                 })
            Assert.False(AppointmentReminder.ShouldSchedule(status, Start, Start.AddDays(-1)));

        // Approved (or created approved) with less than 2 h left: no late reminder,
        // even with more than 15 minutes to go.
        Assert.False(AppointmentReminder.ShouldSchedule(BusinessStatuses.Approved, Start, Start.AddMinutes(-119)));
        Assert.False(AppointmentReminder.ShouldSchedule(BusinessStatuses.Approved, Start, Start.AddMinutes(-30)));
        Assert.False(AppointmentReminder.ShouldSchedule(BusinessStatuses.Approved, Start, Start.AddHours(1)));
    }

    [Fact]
    public void Delay_is_until_two_hours_before_the_start()
    {
        Assert.Equal(TimeSpan.FromHours(22), AppointmentReminder.Delay(Start, Start.AddDays(-1)));
        Assert.Equal(TimeSpan.Zero, AppointmentReminder.Delay(Start, Start.AddHours(-2)));
        Assert.Equal(TimeSpan.Zero, AppointmentReminder.Delay(Start, Start.AddHours(-1)));
    }

    [Fact]
    public void Sent_when_still_approved_at_the_scheduled_start()
    {
        var at = Start.AddHours(-2);

        Assert.True(AppointmentReminder.ShouldSend(Appointment(), Start, at));
        // Read back from the database with less precision, or in another offset.
        Assert.True(AppointmentReminder.ShouldSend(Appointment(Start.AddTicks(-3)), Start, at));
        Assert.True(AppointmentReminder.ShouldSend(Appointment(Start.ToOffset(TimeSpan.FromHours(1))), Start, at));
        // A late job while at least 15 minutes are left.
        Assert.True(AppointmentReminder.ShouldSend(Appointment(), Start, Start.AddMinutes(-15)));
    }

    [Fact]
    public void Not_sent_when_changed_cancelled_deleted_or_too_late()
    {
        var at = Start.AddHours(-2);

        Assert.False(AppointmentReminder.ShouldSend(null, Start, at));
        Assert.False(AppointmentReminder.ShouldSend(Appointment(status: BusinessStatuses.Canceled), Start, at));
        Assert.False(AppointmentReminder.ShouldSend(Appointment(status: BusinessStatuses.Declined), Start, at));
        Assert.False(AppointmentReminder.ShouldSend(Appointment(status: BusinessStatuses.Pending), Start, at));
        Assert.False(AppointmentReminder.ShouldSend(Appointment(isDeleted: true), Start, at));
        // Moved: the new start has its own reminder.
        Assert.False(AppointmentReminder.ShouldSend(Appointment(Start.AddHours(1)), Start, at));
        // A late job with less than 15 minutes left, or after the start.
        Assert.False(AppointmentReminder.ShouldSend(Appointment(), Start, Start.AddMinutes(-14)));
        Assert.False(AppointmentReminder.ShouldSend(Appointment(), Start, Start.AddMinutes(5)));
    }

    [Fact]
    public void Texts_in_the_device_language_with_the_croatian_time()
    {
        var at = Start.AddHours(-2);

        Assert.Equal(new PushText("Trening za 2 h", "Danas u 18:00 · Ivan Horvat"),
            AppointmentReminder.Text("hr", Appointment(), at, Zagreb));
        Assert.Equal(new PushText("Training in 2 h", "Today at 18:00 · Ivan Horvat"),
            AppointmentReminder.Text("en-US", Appointment(), at, Zagreb));
        Assert.Equal(new PushText("Allenamento tra 2 h", "Oggi alle 18:00 · Ivan Horvat"),
            AppointmentReminder.Text("it", Appointment(), at, Zagreb));
        Assert.Equal(new PushText("Training in 2 Std.", "Heute um 18:00 · Ivan Horvat"),
            AppointmentReminder.Text("de", Appointment(), at, Zagreb));
        // No or an unsupported language: Croatian.
        Assert.Equal("Trening za 2 h", AppointmentReminder.Text(null, Appointment(), at, Zagreb).Title);
        Assert.Equal("Trening za 2 h", AppointmentReminder.Text("fr", Appointment(), at, Zagreb).Title);
    }

    [Fact]
    public void Summer_time_and_tomorrow()
    {
        // 07:30 in Zagreb in summer (UTC+2).
        var summer = new DateTimeOffset(2026, 7, 1, 5, 30, 0, TimeSpan.Zero);
        Assert.Equal("Danas u 07:30 · Ivan Horvat",
            AppointmentReminder.Text("hr", Appointment(summer), summer.AddHours(-2), Zagreb).Body);

        // 00:30 on the 21st, reminded at 22:30 on the 20th.
        var afterMidnight = new DateTimeOffset(2026, 11, 20, 23, 30, 0, TimeSpan.Zero);
        Assert.Equal("Sutra u 00:30 · Ivan Horvat",
            AppointmentReminder.Text("hr", Appointment(afterMidnight), afterMidnight.AddHours(-2), Zagreb).Body);
        Assert.Equal("Tomorrow at 00:30 · Ivan Horvat",
            AppointmentReminder.Text("en", Appointment(afterMidnight), afterMidnight.AddHours(-2), Zagreb).Body);
    }

    [Fact]
    public void Coaches_in_the_body()
    {
        var at = Start.AddHours(-2);

        Assert.Equal("Danas u 18:00 · Ivan Horvat, Ana Anić",
            AppointmentReminder.Text("hr", Appointment(coaches: ["Ivan Horvat", " Ana Anić "]), at, Zagreb).Body);
        Assert.Equal("Danas u 18:00",
            AppointmentReminder.Text("hr", Appointment(coaches: [" "]), at, Zagreb).Body);
    }

    [Fact]
    public void Message_is_a_notification_on_ios_and_android()
    {
        var at = Start.AddHours(-2);
        var appointment = Appointment();

        var ios = AppointmentReminder.MessageFor(appointment, new("ios-token", "ios", "en"), at, Zagreb);
        Assert.Equal("ios-token", ios.Token);
        Assert.Equal("Training in 2 h", ios.Notification.Title);
        Assert.Equal("Today at 18:00 · Ivan Horvat", ios.Notification.Body);
        Assert.Equal(PushNotificationTypes.AppointmentReminder, ios.Data["type"]);
        Assert.Equal("appointment_reminder", ios.Data["type"]);
        Assert.Equal(AppointmentId.ToString(), ios.Data["appointmentId"]);
        Assert.Equal(2, ios.Data.Count);
        Assert.Equal("default", ios.Apns.Aps.Sound);
        Assert.Equal(AppointmentId.ToString(), ios.Apns.Headers["apns-collapse-id"]);
        Assert.Equal(Start.ToUnixTimeSeconds().ToString(), ios.Apns.Headers["apns-expiration"]);

        var android = AppointmentReminder.MessageFor(appointment, new("android-token", "android", "de"), at, Zagreb);
        Assert.Equal("Training in 2 Std.", android.Notification.Title);
        Assert.Equal("Heute um 18:00 · Ivan Horvat", android.Notification.Body);
        Assert.Equal("appointment_reminder", android.Data["type"]);
        Assert.Equal(TimeSpan.FromHours(2), android.Android.TimeToLive);
        Assert.Equal(AppointmentId.ToString(), android.Android.Notification.Tag);
    }
}
