using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Hubs;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

/// <summary>
/// Marks all messages of the other side as read and tells the sender
/// (read receipts) through the chat hub.
/// </summary>
public class MarkMessagesReadEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHubContext<ChatHub> _hub;

    public MarkMessagesReadEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IHubContext<ChatHub> hub)
    {
        _context = context;
        _currentUserService = currentUserService;
        _hub = hub;
    }

    public override void Configure()
    {
        Post("api/chat/conversations/{id}/read");
        Roles(UserRoles.Coach, UserRoles.Client);
        Options(x => x.WithTags("Chat"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var conversationId = Route<Guid>("id", isRequired: true);
        var userId = _currentUserService.UserId;

        var conversation = await ChatAccess.GetConversation(_context, userId, conversationId, cancellationToken);

        if (conversation is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var readAt = DateTimeOffset.UtcNow;

        var updated = await _context.ChatMessages
            .Where(x => x.ConversationId == conversationId && x.SenderUserId != userId && x.ReadAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(message => message.ReadAt, readAt), cancellationToken);

        if (updated > 0)
        {
            var otherUserId = conversation.Coach.UserId == userId ? conversation.Client.UserId : conversation.Coach.UserId;

            await _hub.Clients.Users(userId.ToString(), otherUserId.ToString())
                .SendAsync(ChatHub.Events.MessagesRead, new MessagesReadEvent(conversationId, userId, readAt),
                    cancellationToken);
        }

        await SendNoContentAsync(cancellationToken);
    }
}
