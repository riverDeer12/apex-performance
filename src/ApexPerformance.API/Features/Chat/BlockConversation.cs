using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Hubs;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.SignalR;

namespace ApexPerformance.API.Features.Chat;

public record BlockConversationRequest(bool IsBlocked);

/// <summary>
/// Client blocks or unblocks the chat with their coach (App Store rule for
/// user content). A blocked chat keeps its messages and is read only for
/// both sides; the coach can't turn it back on, only the client can unblock it.
/// </summary>
public class BlockConversationEndpoint : Endpoint<BlockConversationRequest, ChatConversationResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHubContext<ChatHub> _hub;

    public BlockConversationEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IHubContext<ChatHub> hub)
    {
        _context = context;
        _currentUserService = currentUserService;
        _hub = hub;
    }

    public override void Configure()
    {
        Post("api/chat/conversations/{id}/block");
        Roles(UserRoles.Client);
        Options(x => x.WithTags("Chat"));
    }

    public override async Task HandleAsync(BlockConversationRequest request, CancellationToken cancellationToken)
    {
        var conversationId = Route<Guid>("id", isRequired: true);
        var userId = _currentUserService.UserId;

        var conversation = await ChatAccess.GetConversation(_context, userId, conversationId, cancellationToken);

        // Only the client of the conversation can block it.
        if (conversation is null || conversation.Client.UserId != userId)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        conversation.IsBlockedByClient = request.IsBlocked;

        await _context.SaveChangesAsync(cancellationToken);

        await _hub.Clients.Users(conversation.Coach.UserId.ToString(), conversation.Client.UserId.ToString())
            .SendAsync(ChatHub.Events.ConversationUpdated, conversation.Id, cancellationToken);

        await SendAsync(ChatAccess.ToResponse(conversation, conversation.Coach, conversation.Client),
            cancellation: cancellationToken);
    }
}
