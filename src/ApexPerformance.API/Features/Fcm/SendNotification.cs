using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects.Fcm;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Fcm;

public class SendNotificationEndpoint : Endpoint<SendNotificationRequest, SendNotificationResponse>
{
    private readonly INotificationService _notificationService;
    private readonly ApexPerformanceContext _db;

    public SendNotificationEndpoint(
        INotificationService notificationService,
        ApexPerformanceContext db)
    {
        _notificationService = notificationService;
        _db = db;
    }

    public override void Configure()
    {
        Post("/api/notifications/send");
    }

    public override async Task HandleAsync(SendNotificationRequest request, CancellationToken cancellationToken)
    {
        var deviceToken = await _db.DeviceTokens
            .Where(x => x.UserId == request.UserId)
            .Select(x => x.Token)
            .FirstOrDefaultAsync(cancellationToken);

        if (deviceToken is null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var data = new Dictionary<string, string>
        {
            { "screen", request.TargetScreen ?? "" },
            { "itemId", request.ItemId ?? "" }
        };

        var messageId = await _notificationService.SendToDevice(
            deviceToken,
            request.Title,
            request.Body,
            data
        );

        await SendAsync(new SendNotificationResponse { MessageId = messageId }, cancellation: cancellationToken);
    }
}