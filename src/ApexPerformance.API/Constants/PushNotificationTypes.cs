namespace ApexPerformance.API.Constants;

// Sent in the push "type" data key; the mobile app uses it to open the matching screen.
public static class PushNotificationTypes
{
    public const string AppointmentRequest = "appointment_request";
    public const string AppointmentUpdated = "appointment_updated";
    public const string BodyMeasurement = "body_measurement";

    public static Dictionary<string, string> Data(string type) => new() { ["type"] = type };
}
