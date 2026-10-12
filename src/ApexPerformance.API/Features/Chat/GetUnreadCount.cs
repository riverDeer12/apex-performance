using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

public record UnreadCountResponse(int Count);

/// <summary>
/// Number of unread messages in all conversations, for the menu badge.
/// </summary>
public class GetUnreadCountEndpoint : EndpointWithoutRequest<UnreadCountResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadCountEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/chat/unread-count");
        Roles(UserRoles.Coach, UserRoles.Client);
        Options(x => x.WithTags("Chat"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var count = await ChatAccess.VisibleConversations(_context, userId)
            .SelectMany(x => x.Messages)
            .CountAsync(x => x.SenderUserId != userId && x.ReadAt == null, cancellationToken);

        await SendAsync(new UnreadCountResponse(count), cancellation: cancellationToken);
    }
}
