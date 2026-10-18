using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

/// <summary>
/// Conversations of the logged user, newest first, disabled ones included
/// (they are read only).
/// </summary>
public class GetConversationsEndpoint : EndpointWithoutRequest<List<ChatConversationResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatEncryptionService _encryption;

    public GetConversationsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IChatEncryptionService encryption)
    {
        _context = context;
        _currentUserService = currentUserService;
        _encryption = encryption;
    }

    public override void Configure()
    {
        Get("api/chat/conversations");
        Roles(UserRoles.Coach, UserRoles.Client);
        Options(x => x.WithTags("Chat"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var conversations = await ChatAccess.VisibleConversations(_context, userId)
            .AsNoTracking()
            .Include(x => x.Coach)
            .Include(x => x.Client)
            .Select(x => new
            {
                Conversation = x,
                LastMessage = x.Messages.OrderByDescending(message => message.CreatedAt).FirstOrDefault(),
                UnreadCount = x.Messages.Count(message => message.SenderUserId != userId && message.ReadAt == null)
            })
            .ToListAsync(cancellationToken);

        var response = conversations
            .OrderByDescending(x => x.Conversation.LastMessageAt ?? x.Conversation.CreatedAt)
            .Select(x => new ChatConversationResponse(
                x.Conversation.Id,
                ChatAccess.Participant(x.Conversation.Coach),
                ChatAccess.Participant(x.Conversation.Client),
                ChatAccess.CanWrite(x.Conversation),
                x.Conversation.LastMessageAt,
                x.LastMessage is null ? null : ChatAccess.ToResponse(x.LastMessage, _encryption),
                x.UnreadCount,
                x.Conversation.IsBlockedByClient))
            .ToList();

        await SendAsync(response, cancellation: cancellationToken);
    }
}
