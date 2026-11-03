using ApexPerformance.API.Constants;
using FirebaseAdmin.Messaging;

namespace ApexPerformance.API.Services.Interfaces;

public interface INotificationService
{
    Task<string> SendToDevice(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null);

    Task<BatchResponse?> SendToMultipleDevices(
        List<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null);

    /// <summary>
    /// Sends each device the text in the language of its app.
    /// </summary>
    Task SendLocalized(
        IReadOnlyCollection<PushRecipient> recipients,
        Func<string, PushText> text,
        Dictionary<string, string>? data = null);

    Task<string> SendToTopic(
        string topic,
        string title,
        string body);
}