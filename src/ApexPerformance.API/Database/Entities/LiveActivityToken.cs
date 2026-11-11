namespace ApexPerformance.API.Database.Entities;

public enum LiveActivityTokenKind
{
    // Lets the backend start a training's Live Activity on the device (iOS 17.2+).
    PushToStart,
    // Updates and ends one running Live Activity, the one of TrainingId.
    Update
}

/// <summary>
/// ActivityKit push token of an iOS device, so the backend can start, update
/// and end the Live Activity of a training in progress through Firebase.
/// Belongs to the device's FCM token and goes away with it.
/// One push-to-start token per device, one update token per device and training.
/// </summary>
public class LiveActivityToken
{
    public Guid Id { get; set; }
    public Guid DeviceTokenId { get; set; }
    public DeviceToken? DeviceToken { get; set; }
    public LiveActivityTokenKind Kind { get; set; }
    // Only for update tokens. Not a foreign key: trainings are soft deleted.
    public Guid? TrainingId { get; set; }
    public required string Token { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
