using ApexPerformance.API.Services.Interfaces;
using FirebaseAdmin.Messaging;

namespace ApexPerformance.API.Services.Implementation;

public class NotificationService : INotificationService
{
    public async Task<string> SendToDevice(string deviceToken, string title, string body,
        Dictionary<string, string>? data = null)
    {
        var message = new Message()
        {
            Token = deviceToken,
            Notification = new Notification()
            {
                Title = title,
                Body = body,
            },
            Data = data ?? new Dictionary<string, string>(),

            // iOS-specific config
            Apns = new ApnsConfig()
            {
                Aps = new Aps()
                {
                    Sound = "default",
                    Badge = 1,
                }
            }
        };

        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        
        return response; // Returns message ID on success
    }

    public async Task<BatchResponse?> SendToMultipleDevices(List<string> deviceTokens, string title, string body,
        Dictionary<string, string>? data = null)
    {
        // Firebase throws on an empty token list, which would abort the calling job
        // and skip any notifications it sends afterwards.
        if (deviceTokens.Count is 0)
            return null;

        var message = new MulticastMessage()
        {
            Tokens = deviceTokens,
            Notification = new Notification()
            {
                Title = title,
                Body = body,
            },
            Data = data ?? new Dictionary<string, string>(),
            Apns = new ApnsConfig()
            {
                Aps = new Aps() { Sound = "default" }
            }
        };

        return await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
    }

    public async Task<string> SendToTopic(string topic, string title, string body)
    {
        var message = new Message()
        {
            Topic = topic,
            Notification = new Notification()
            {
                Title = title,
                Body = body,
            }
        };

        return await FirebaseMessaging.DefaultInstance.SendAsync(message);
    }
}