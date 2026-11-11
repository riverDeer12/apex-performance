using ApexPerformance.API.Utilities;
using FirebaseAdmin.Messaging;

namespace ApexPerformance.Tests;

public class LiveActivityPushTests
{
    private static readonly Guid TrainingId = Guid.Parse("5b0c3f0e-8a7d-4c1b-9e2f-1a2b3c4d5e6f");
    private static readonly Guid ClientUser = Guid.NewGuid();
    private static readonly Guid CoachUser = Guid.NewGuid();
    private static readonly Guid OtherUser = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 11, 11, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Started = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ended = new(2026, 11, 11, 11, 0, 0, TimeSpan.Zero);

    // Seconds between 1970-01-01 and 2001-01-01 (NSTimeIntervalSince1970).
    private const long SwiftEpochInUnix = 978307200;

    private static LiveActivityPush.Training Training(DateTimeOffset? startedAt = null, DateTimeOffset? endedAt = null,
        bool isCompleted = false, bool isDeleted = false, DateTimeOffset? previousStartedAt = null)
        => new(TrainingId, "Legs", startedAt, endedAt, isCompleted, isDeleted, previousStartedAt, ClientUser,
            "Ana Anić", "Marko Marić");

    private static LiveActivityPush.Device Device(Guid userId, string fcm, string? language = "en",
        string? pushToStart = null, string? update = null)
        => new(Guid.NewGuid(), fcm, userId, language, pushToStart, update);

    private static Dictionary<string, object> ApsOf(Message message) =>
        (Dictionary<string, object>)message.Apns.Aps.CustomData;

    private static Dictionary<string, object> StateOf(Message message) =>
        (Dictionary<string, object>)ApsOf(message)["content-state"];

    private static Dictionary<string, object> AttributesOf(Message message) =>
        (Dictionary<string, object>)ApsOf(message)["attributes"];

    [Fact]
    public void Dates_are_seconds_since_2001_like_Swift()
    {
        Assert.Equal(0, LiveActivityPush.SwiftDate(new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        Assert.Equal(Started.ToUnixTimeSeconds() - SwiftEpochInUnix, LiveActivityPush.SwiftDate(Started));
        // Time zone of the value doesn't matter.
        Assert.Equal(LiveActivityPush.SwiftDate(Started),
            LiveActivityPush.SwiftDate(Started.ToOffset(TimeSpan.FromHours(1))));
        Assert.Equal(0.25, LiveActivityPush.SwiftDate(new DateTimeOffset(2001, 1, 1, 0, 0, 0, 250, TimeSpan.Zero)));
    }

    [Fact]
    public void Running_state_has_no_end_and_upper_case_id()
    {
        var state = LiveActivityPush.ContentState(Training(Started));

        Assert.Equal((double)(Started.ToUnixTimeSeconds() - SwiftEpochInUnix), state["startedAt"]);
        Assert.Equal("5B0C3F0E-8A7D-4C1B-9E2F-1A2B3C4D5E6F", state["trainingId"]);
        Assert.Equal("Legs", state["trainingName"]);
        Assert.False(state.ContainsKey("endedAt"));
    }

    [Fact]
    public void Start_for_client_shows_coach_and_no_end_button_in_device_language()
    {
        var message = LiveActivityPush.StartMessage(Training(Started), "fcm-client", "pts-client", "de",
            forClient: true, Now);

        Assert.Equal("fcm-client", message.Token);
        Assert.Equal("pts-client", message.Apns.LiveActivityToken);
        Assert.Equal("10", message.Apns.Headers["apns-priority"]);

        var aps = ApsOf(message);
        Assert.Equal("start", aps["event"]);
        Assert.Equal(Now.ToUnixTimeSeconds(), aps["timestamp"]);
        Assert.Equal("TrainingActivityAttributes", aps["attributes-type"]);

        var attributes = AttributesOf(message);
        Assert.Equal("5B0C3F0E-8A7D-4C1B-9E2F-1A2B3C4D5E6F", attributes["key"]);
        Assert.Equal("Marko Marić", attributes["clientName"]);
        Assert.Equal("Training läuft", attributes["inProgressText"]);
        Assert.Equal("Ende", attributes["endedText"]);
        Assert.Equal("Training beenden", attributes["endText"]);
        Assert.Equal(false, attributes["showsEndButton"]);

        // Apple needs an alert to start an activity.
        Assert.Equal("Training läuft", message.Apns.Aps.Alert.Title);
        Assert.Equal("Legs · Marko Marić", message.Apns.Aps.Alert.Body);
    }

    [Fact]
    public void Start_for_staff_shows_client_and_end_button()
    {
        var message = LiveActivityPush.StartMessage(Training(Started), "fcm", "pts", "en", forClient: false, Now);

        var attributes = AttributesOf(message);
        Assert.Equal("Ana Anić", attributes["clientName"]);
        Assert.Equal("Training in progress", attributes["inProgressText"]);
        Assert.Equal("End", attributes["endedText"]);
        Assert.Equal("End training", attributes["endText"]);
        Assert.Equal(true, attributes["showsEndButton"]);
    }

    [Theory]
    [InlineData(null, "Trening je u tijeku")]
    [InlineData("hr", "Trening je u tijeku")]
    [InlineData("it", "Allenamento in corso")]
    [InlineData("en-US", "Training in progress")]
    [InlineData("fr", "Trening je u tijeku")]
    public void Start_texts_fall_back_to_Croatian(string? language, string inProgress)
    {
        var message = LiveActivityPush.StartMessage(Training(Started), "fcm", "pts", language, true, Now);

        Assert.Equal(inProgress, AttributesOf(message)["inProgressText"]);
    }

    [Fact]
    public void Update_sends_the_new_state()
    {
        var message = LiveActivityPush.UpdateMessage(Training(Started) with { Name = "Legs & core" }, "fcm",
            "update-token", Now);

        Assert.Equal("update-token", message.Apns.LiveActivityToken);
        Assert.Null(message.Apns.Aps.Alert);
        Assert.Equal("update", ApsOf(message)["event"]);
        Assert.Equal("Legs & core", StateOf(message)["trainingName"]);
        Assert.False(ApsOf(message).ContainsKey("attributes"));
    }

    [Fact]
    public void Ended_training_shows_its_duration_for_15_minutes()
    {
        var message = LiveActivityPush.EndMessage(Training(Started, Ended, isCompleted: true), "fcm", "update-token",
            Now);

        var aps = ApsOf(message);
        Assert.Equal("end", aps["event"]);
        Assert.Equal(Ended.AddMinutes(15).ToUnixTimeSeconds(), aps["dismissal-date"]);
        Assert.Equal((double)(Ended.ToUnixTimeSeconds() - SwiftEpochInUnix), StateOf(message)["endedAt"]);
        Assert.Equal((double)(Started.ToUnixTimeSeconds() - SwiftEpochInUnix), StateOf(message)["startedAt"]);
    }

    [Fact]
    public void Cleared_times_end_right_away_with_the_previous_start()
    {
        var message = LiveActivityPush.EndMessage(Training(previousStartedAt: Started), "fcm", "update-token", Now);

        Assert.Equal(Now.ToUnixTimeSeconds(), ApsOf(message)["dismissal-date"]);
        Assert.Equal((double)(Started.ToUnixTimeSeconds() - SwiftEpochInUnix), StateOf(message)["startedAt"]);
        Assert.False(StateOf(message).ContainsKey("endedAt"));
    }

    [Fact]
    public void Deleted_or_completed_without_end_end_right_away()
    {
        var deleted = LiveActivityPush.EndMessage(Training(Started, isDeleted: true), "f", "u", Now);
        var completed = LiveActivityPush.EndMessage(Training(Started, isCompleted: true), "f", "u", Now);

        Assert.Equal(Now.ToUnixTimeSeconds(), ApsOf(deleted)["dismissal-date"]);
        Assert.Equal(Now.ToUnixTimeSeconds(), ApsOf(completed)["dismissal-date"]);
    }

    [Fact]
    public void Started_training_starts_on_client_and_coach_other_devices_only()
    {
        var clientPhone = Device(ClientUser, "client-phone", pushToStart: "pts-1");
        var clientIpad = Device(ClientUser, "client-ipad", pushToStart: null); // iOS < 17.2
        var clientShowing = Device(ClientUser, "client-showing", pushToStart: "pts-2", update: "upd-2");
        var coachThisPhone = Device(CoachUser, "coach-this", pushToStart: "pts-3");
        var coachOtherPhone = Device(CoachUser, "coach-other", pushToStart: "pts-4");
        var otherUser = Device(OtherUser, "other", pushToStart: "pts-5");

        var sends = LiveActivityPush.Plan(Training(Started),
            [clientPhone, clientIpad, clientShowing, coachThisPhone, coachOtherPhone, otherUser],
            wasRunning: false, CoachUser, "coach-this", Now);

        Assert.Equal(3, sends.Count);
        Assert.Contains(sends, x => x.DeviceTokenId == clientPhone.DeviceTokenId && x.Event == "start" &&
                                    x.ActivityToken == "pts-1" &&
                                    Equals(AttributesOf(x.Message)["showsEndButton"], false));
        Assert.Contains(sends, x => x.DeviceTokenId == clientShowing.DeviceTokenId && x.Event == "update" &&
                                    x.ActivityToken == "upd-2");
        Assert.Contains(sends, x => x.DeviceTokenId == coachOtherPhone.DeviceTokenId && x.Event == "start" &&
                                    Equals(AttributesOf(x.Message)["showsEndButton"], true));
    }

    [Fact]
    public void Change_of_a_running_training_only_updates()
    {
        var clientPhone = Device(ClientUser, "client-phone", pushToStart: "pts-1");
        var showing = Device(OtherUser, "coach-2", update: "upd");

        var sends = LiveActivityPush.Plan(Training(Started), [clientPhone, showing], wasRunning: true, CoachUser,
            null, Now);

        var send = Assert.Single(sends);
        Assert.Equal("update", send.Event);
        Assert.Equal(showing.DeviceTokenId, send.DeviceTokenId);
    }

    [Fact]
    public void Ended_training_ends_everywhere_it_shows_and_starts_nothing()
    {
        var clientPhone = Device(ClientUser, "client-phone", pushToStart: "pts-1");
        var clientShowing = Device(ClientUser, "client-showing", update: "upd-1");
        var coachShowing = Device(CoachUser, "coach", pushToStart: "pts-2", update: "upd-2");

        var sends = LiveActivityPush.Plan(Training(Started, Ended, isCompleted: true),
            [clientPhone, clientShowing, coachShowing], wasRunning: true, CoachUser, "coach", Now);

        Assert.Equal(2, sends.Count);
        Assert.All(sends, x => Assert.Equal("end", x.Event));
        Assert.DoesNotContain(sends, x => x.DeviceTokenId == clientPhone.DeviceTokenId);
    }

    [Fact]
    public void Web_start_starts_on_all_coach_devices()
    {
        var coachPhone = Device(CoachUser, "coach-phone", pushToStart: "pts-1");

        var sends = LiveActivityPush.Plan(Training(Started), [coachPhone], wasRunning: false, CoachUser, null, Now);

        Assert.Equal("start", Assert.Single(sends).Event);
    }

    [Fact]
    public void Same_device_listed_twice_gets_one_push()
    {
        var phone = Device(ClientUser, "client-phone", pushToStart: "pts-1");

        var sends = LiveActivityPush.Plan(Training(Started), [phone, phone], wasRunning: false, null, null, Now);

        Assert.Single(sends);
    }
}
