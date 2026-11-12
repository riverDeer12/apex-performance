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
    // Data also has "trainingId" of the training to open.
    public const string Training = "training";
    // To the coach who started a training still running after an hour, with
    // "End training". Data also has "trainingId" and "startedAt" (ISO 8601).
    public const string TrainingEndReminder = "training_end_reminder";
    // Data also has "conversationId" of the conversation to open.
    public const string ChatMessage = "chat_message";

    public static Dictionary<string, string> Data(string type) => new() { ["type"] = type };
}
