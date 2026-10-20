using System.Text.Json;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Notifications;

public record NotificationResponse(
    Guid Id,
    // PushNotificationTypes value, opens the same screen as the push.
    string Type,
    string Title,
    string Body,
    // Push data (type and ids like trainingId or conversationId).
    Dictionary<string, string> Data,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public record NotificationsUnreadCountResponse(int Count);

/// <summary>
/// Notifications of the logged user, newest first (the latest 100).
/// </summary>
public class GetNotificationsEndpoint : EndpointWithoutRequest<List<NotificationResponse>>
{
    private const int MaxCount = 100;

    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/notifications");
        Options(x => x.WithTags("Notifications"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var notifications = await _context.UserNotifications
            .AsNoTracking()
            .Where(x => x.UserId == _currentUserService.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxCount)
            .ToListAsync(cancellationToken);

        await SendAsync(notifications.Select(x => new NotificationResponse(x.Id, x.Type, x.Title, x.Body,
                ParseData(x.Data), x.CreatedAt, x.ReadAt)).ToList(),
            cancellation: cancellationToken);
    }

    private static Dictionary<string, string> ParseData(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}

/// <summary>
/// Number of unread notifications, for the badge on the bell.
/// </summary>
public class GetNotificationsUnreadCountEndpoint : EndpointWithoutRequest<NotificationsUnreadCountResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsUnreadCountEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/notifications/unread-count");
        Options(x => x.WithTags("Notifications"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var count = await _context.UserNotifications
            .CountAsync(x => x.UserId == _currentUserService.UserId && x.ReadAt == null, cancellationToken);

        await SendAsync(new NotificationsUnreadCountResponse(count), cancellation: cancellationToken);
    }
}

/// <summary>
/// Marks one notification as read (opened from the list).
/// </summary>
public class ReadNotificationEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ReadNotificationEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Post("api/notifications/{id}/read");
        Options(x => x.WithTags("Notifications"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);
        var now = DateTimeOffset.UtcNow;

        var updated = await _context.UserNotifications
            .Where(x => x.Id == id && x.UserId == _currentUserService.UserId && x.ReadAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(notification => notification.ReadAt, now), cancellationToken);

        if (updated == 0 && !await _context.UserNotifications
                .AnyAsync(x => x.Id == id && x.UserId == _currentUserService.UserId, cancellationToken))
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        await SendNoContentAsync(cancellationToken);
    }
}

/// <summary>
/// Marks all notifications of the logged user as read.
/// </summary>
public class ReadAllNotificationsEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ReadAllNotificationsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Post("api/notifications/read-all");
        Options(x => x.WithTags("Notifications"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        await _context.UserNotifications
            .Where(x => x.UserId == _currentUserService.UserId && x.ReadAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(notification => notification.ReadAt, now), cancellationToken);

        await SendNoContentAsync(cancellationToken);
    }
}

/// <summary>
/// Deletes the read notifications of the logged user (clears the list).
/// </summary>
public class DeleteReadNotificationsEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteReadNotificationsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Delete("api/notifications/read");
        Options(x => x.WithTags("Notifications"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        await _context.UserNotifications
            .Where(x => x.UserId == _currentUserService.UserId && x.ReadAt != null)
            .ExecuteDeleteAsync(cancellationToken);

        await SendNoContentAsync(cancellationToken);
    }
}
