using ApexPerformance.API.Shared.DataTransferObjects.Fcm;

namespace ApexPerformance.API.Services.Interfaces;

public interface IFcmService
{
    Task<SendNotificationResponse> SendAsync(SendNotificationRequest request);
    Task<SendNotificationResponse> SendToTopicAsync(string topic, string title, string body);
}