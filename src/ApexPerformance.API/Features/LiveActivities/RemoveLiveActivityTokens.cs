using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.LiveActivities;

/// <param name="DeviceToken">FCM registration token of the iPhone.</param>
/// <param name="TrainingId">Only the update token of this training; all tokens of the device when null.</param>
public record RemoveLiveActivityTokensRequest(string DeviceToken, Guid? TrainingId = null);

/// <summary>
/// The app logs out (all tokens of the device) or its activity of a
/// training ended (that training's update token). Tokens of other users'
/// devices are left alone. Removing nothing is fine too.
/// </summary>
public class RemoveLiveActivityTokensEndpoint : Endpoint<RemoveLiveActivityTokensRequest>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveLiveActivityTokensEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        // POST, the FCM token doesn't belong in a URL.
        Post("api/live-activity-tokens/remove");
        Options(x => x.WithTags("Notifications"));
    }

    public override async Task HandleAsync(RemoveLiveActivityTokensRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var tokens = _context.LiveActivityTokens
            .Where(x => x.DeviceToken!.Token == request.DeviceToken && x.DeviceToken.UserId == userId);

        if (request.TrainingId is not null)
            tokens = tokens.Where(x => x.Kind == LiveActivityTokenKind.Update && x.TrainingId == request.TrainingId);

        await tokens.ExecuteDeleteAsync(cancellationToken);

        await SendOkAsync(cancellationToken);
    }
}
