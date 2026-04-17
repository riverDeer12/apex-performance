namespace ApexPerformance.API.Shared.DataTransferObjects.Fcm;

public class SendNotificationRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
}