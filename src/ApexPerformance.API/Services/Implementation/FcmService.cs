using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects.Fcm;
using FirebaseAdmin.Messaging;

namespace ApexPerformance.API.Services.Implementation;

public class FcmService : IFcmService
{
    private readonly FirebaseMessaging _messaging;

    public FcmService()
    {
        _messaging = FirebaseMessaging.DefaultInstance;
    }

    public async Task<SendNotificationResponse> SendAsync(SendNotificationRequest request)
    {
        try
        {
            var message = new Message
            {
                Token = request.DeviceToken,
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Body
                },
                Data = request.Data,
                Android = new AndroidConfig
                {
                    Priority = Priority.High
                },
                Apns = new ApnsConfig
                {
                    Aps = new Aps { Sound = "default" }
                }
            };

            var messageId = await _messaging.SendAsync(message);

            return new SendNotificationResponse { Success = true, MessageId = messageId };
        }
        catch (FirebaseMessagingException ex)
        {
            return new SendNotificationResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<SendNotificationResponse> SendToTopicAsync(string topic, string title, string body)
    {
        try
        {
            var message = new Message
            {
                Topic = topic,
                Notification = new Notification { Title = title, Body = body }
            };

            var messageId = await _messaging.SendAsync(message);
            
            return new SendNotificationResponse { Success = true, MessageId = messageId };
        }
        catch (FirebaseMessagingException ex)
        {
            return new SendNotificationResponse { Success = false, Error = ex.Message };
        }
    }
}