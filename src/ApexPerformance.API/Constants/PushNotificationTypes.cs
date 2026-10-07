namespace ApexPerformance.API.Constants;

// Sent in the push "type" data key; the mobile app uses it to open the matching screen.
public static class PushNotificationTypes
{
    public const string AppointmentRequest = "appointment_request";
    public const string AppointmentUpdated = "appointment_updated";
    public const string BodyMeasurement = "body_measurement";
    public const string ClientGoal = "client_goal";
    public const string MonthlyReview = "monthly_review";
    public const string MonthlyReviewReminder = "monthly_review_reminder";

    public static Dictionary<string, string> Data(string type) => new() { ["type"] = type };
}
