using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

/// <summary>
/// People the logged user can chat with: clients of a coach, coaches of a
/// client. ConversationId is null when they haven't started a chat yet.
/// </summary>
public class GetContactsEndpoint : EndpointWithoutRequest<List<ChatContactResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetContactsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/chat/contacts");
        Roles(UserRoles.Coach, UserRoles.Client);
        Options(x => x.WithTags("Chat"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var links = await _context.CoachClients
            .AsNoTracking()
            .Where(x => x.Coach.UserId == userId || x.Client.UserId == userId)
            .Select(x => new
            {
                IsCoach = x.Coach.UserId == userId,
                x.Coach,
                x.Client,
                Conversation = _context.ChatConversations
                    .Where(conversation => conversation.CoachId == x.CoachId && conversation.ClientId == x.ClientId)
                    .Select(conversation => new { conversation.Id, conversation.IsEnabled })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var response = links
            .Select(x =>
            {
                var participant = x.IsCoach ? ChatAccess.Participant(x.Client) : ChatAccess.Participant(x.Coach);

                return new ChatContactResponse(participant.Id, participant.UserId, participant.FullName,
                    x.Conversation?.Id,
                    // No conversation yet means the chat is open.
                    x.Conversation?.IsEnabled ?? true);
            })
            .OrderBy(x => x.FullName)
            .ToList();

        await SendAsync(response, cancellation: cancellationToken);
    }
}
