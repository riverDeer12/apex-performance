using FirebaseAdmin.Messaging;

namespace ApexPerformance.API.Services.Interfaces;

public interface INotificationService
{
    Task<string> SendToDevice(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null);

    Task<BatchResponse> SendToMultipleDevices(
        List<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null);

    Task<string> SendToTopic(
        string topic,
        string title,
        string body);
}