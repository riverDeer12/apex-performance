using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

public record GetMessagesRequest(DateTimeOffset? Before, int? Take);

/// <summary>
/// Messages of a conversation, oldest first. Without "before" the newest
/// messages are returned; older ones are loaded with the time of the
/// oldest loaded message as "before".
/// </summary>
public class GetMessagesEndpoint : Endpoint<GetMessagesRequest, List<ChatMessageResponse>>
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;

    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatEncryptionService _encryption;

    public GetMessagesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IChatEncryptionService encryption)
    {
        _context = context;
        _currentUserService = currentUserService;
        _encryption = encryption;
    }

    public override void Configure()
    {
        Get("api/chat/conversations/{id}/messages");
        Roles(UserRoles.Coach, UserRoles.Client);
        Options(x => x.WithTags("Chat"));
    }

    public override async Task HandleAsync(GetMessagesRequest request, CancellationToken cancellationToken)
    {
        var conversationId = Route<Guid>("id", isRequired: true);

        var conversation = await ChatAccess.GetConversation(_context, _currentUserService.UserId, conversationId,
            cancellationToken);

        if (conversation is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var query = _context.ChatMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId);

        if (request.Before is not null)
            query = query.Where(x => x.CreatedAt < request.Before);

        var messages = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(request.Take ?? DefaultTake, 1, MaxTake))
            .ToListAsync(cancellationToken);

        await SendAsync(messages
                .OrderBy(x => x.CreatedAt)
                .Select(x => ChatAccess.ToResponse(x, _encryption))
                .ToList(),
            cancellation: cancellationToken);
    }
}
