namespace ApexPerformance.API.Shared.DataTransferObjects.Fcm;

public class SendNotificationResponse
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? Error { get; set; }
}