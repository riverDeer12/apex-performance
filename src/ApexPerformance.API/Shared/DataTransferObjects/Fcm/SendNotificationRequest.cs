namespace ApexPerformance.API.Shared.DataTransferObjects.Fcm;

public class SendNotificationRequest
{
    public Guid UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string TargetScreen { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
}