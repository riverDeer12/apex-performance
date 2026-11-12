using ApexPerformance.API.Constants;
using ApexPerformance.API.Utilities;

namespace ApexPerformance.Tests;

public class TrainingEndReminderTests
{
    private static readonly Guid TrainingId = Guid.Parse("5b0c3f0e-8a7d-4c1b-9e2f-1a2b3c4d5e6f");
    private static readonly DateTimeOffset Started = new(2026, 11, 11, 10, 0, 0, 250, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = Started.AddMinutes(1);

    private static TrainingEndReminder.Training Training(DateTimeOffset? startedAt = null, DateTimeOffset? endedAt = null,
        bool isCompleted = false, bool isDeleted = false, string client = "Ana Anić")
        => new(TrainingId, "Legs", client, startedAt, endedAt, isCompleted, isDeleted);

    [Fact]
    public void Scheduled_for_a_new_start()
    {
        Assert.True(TrainingEndReminder.ShouldSchedule(null, Started, null, false, Now));
        // Start time changed while running: the reminder follows it.
        Assert.True(TrainingEndReminder.ShouldSchedule(Started, Started.AddMinutes(-10), null, false, Now));
    }

    [Fact]
    public void Not_scheduled_without_a_new_running_start()
    {
        // Ended, cleared or completed.
        Assert.False(TrainingEndReminder.ShouldSchedule(Started, Started, Started.AddMinutes(30), false, Now));
        Assert.False(TrainingEndReminder.ShouldSchedule(null, Started, Started.AddMinutes(30), false, Now));
        Assert.False(TrainingEndReminder.ShouldSchedule(Started, null, null, false, Now));
        Assert.False(TrainingEndReminder.ShouldSchedule(null, Started, null, true, Now));
        // Same start (reopened after an end): the reminder of that start is already scheduled.
        Assert.False(TrainingEndReminder.ShouldSchedule(Started, Started, null, false, Now));
        // Database precision.
        Assert.False(TrainingEndReminder.ShouldSchedule(Started, Started.AddTicks(5), null, false, Now));
        // A start set more than an hour back while the coach is editing the times.
        Assert.False(TrainingEndReminder.ShouldSchedule(null, Started, null, false, Started.AddMinutes(61)));
    }

    [Fact]
    public void Delay_is_an_hour_after_the_start()
    {
        Assert.Equal(TimeSpan.FromMinutes(59), TrainingEndReminder.Delay(Started, Now));
        Assert.Equal(TimeSpan.FromHours(1), TrainingEndReminder.Delay(Started, Started));
        Assert.Equal(TimeSpan.Zero, TrainingEndReminder.Delay(Started, Started.AddHours(2)));
    }

    [Fact]
    public void Sent_when_still_running_with_the_scheduled_start()
    {
        var at = Started.AddHours(1);

        Assert.True(TrainingEndReminder.ShouldSend(Training(Started), Started, at));
        // Read back from the database with less precision.
        Assert.True(TrainingEndReminder.ShouldSend(Training(Started.AddTicks(-3)), Started, at));
        // In another time zone it is the same start.
        Assert.True(TrainingEndReminder.ShouldSend(Training(Started.ToOffset(TimeSpan.FromHours(1))), Started, at));
    }

    [Fact]
    public void Skipped_when_ended_changed_or_gone()
    {
        var at = Started.AddHours(1);

        Assert.False(TrainingEndReminder.ShouldSend(null, Started, at));
        Assert.False(TrainingEndReminder.ShouldSend(Training(Started, Started.AddMinutes(45)), Started, at));
        Assert.False(TrainingEndReminder.ShouldSend(Training(Started, isCompleted: true), Started, at));
        Assert.False(TrainingEndReminder.ShouldSend(Training(Started, isDeleted: true), Started, at));
        Assert.False(TrainingEndReminder.ShouldSend(Training(), Started, at));
        // Restarted or start changed: that start has its own reminder.
        Assert.False(TrainingEndReminder.ShouldSend(Training(Started.AddMinutes(20)), Started, at));
        // The job ran so late that the training can't be ended with that start any more.
        Assert.False(TrainingEndReminder.ShouldSend(Training(Started), Started, Started.AddHours(7)));
    }

    [Theory]
    [InlineData("hr", "Trening još traje", "Legs · Ana Anić traje već sat vremena. Želiš li ga završiti?")]
    [InlineData("en", "Training still running", "Legs · Ana Anić has been running for an hour. Do you want to end this training?")]
    [InlineData("it", "Allenamento ancora in corso", "Legs · Ana Anić è in corso da un'ora. Vuoi terminare l'allenamento?")]
    [InlineData("de", "Training läuft noch", "Legs · Ana Anić läuft schon seit einer Stunde. Möchtest du das Training beenden?")]
    [InlineData("de-AT", "Training läuft noch", "Legs · Ana Anić läuft schon seit einer Stunde. Möchtest du das Training beenden?")]
    [InlineData(null, "Trening još traje", "Legs · Ana Anić traje već sat vremena. Želiš li ga završiti?")]
    [InlineData("fr", "Trening još traje", "Legs · Ana Anić traje već sat vremena. Želiš li ga završiti?")]
    public void Texts_in_the_device_language(string? language, string title, string body)
    {
        var text = TrainingEndReminder.Text(language, "Legs", "Ana Anić");

        Assert.Equal(title, text.Title);
        Assert.Equal(body, text.Body);
    }

    [Fact]
    public void Without_a_client_name_only_the_training()
        => Assert.Equal("Legs has been running for an hour. Do you want to end this training?",
            TrainingEndReminder.Text("en", "Legs", " ").Body);

    [Fact]
    public void Ios_message_has_the_category_with_the_end_action()
    {
        var message = TrainingEndReminder.MessageFor(Training(Started),
            new TrainingEndReminder.Device("ios-token", "ios", "en"));

        Assert.Equal("ios-token", message.Token);
        Assert.Equal("Training still running", message.Notification.Title);
        Assert.Equal("TRAINING_END_REMINDER", message.Apns.Aps.Category);
        Assert.Equal("default", message.Apns.Aps.Sound);
        Assert.Equal(PushNotificationTypes.TrainingEndReminder, message.Data["type"]);
        Assert.Equal(TrainingId.ToString(), message.Data["trainingId"]);
        Assert.Equal("2026-11-11T10:00:00.250Z", message.Data["startedAt"]);
        Assert.False(message.Data.ContainsKey("title"));
    }

    [Fact]
    public void Android_message_is_data_only_so_the_app_adds_the_action()
    {
        var message = TrainingEndReminder.MessageFor(Training(Started.ToOffset(TimeSpan.FromHours(1))),
            new TrainingEndReminder.Device("android-token", "android", "hr"));

        Assert.Null(message.Notification);
        Assert.Null(message.Apns);
        Assert.Equal("Trening još traje", message.Data["title"]);
        Assert.Equal("Legs · Ana Anić traje već sat vremena. Želiš li ga završiti?", message.Data["body"]);
        Assert.Equal("training_end_reminder", message.Data["type"]);
        Assert.Equal("2026-11-11T10:00:00.250Z", message.Data["startedAt"]);
        Assert.Equal(FirebaseAdmin.Messaging.Priority.High, message.Android.Priority);
    }
}
