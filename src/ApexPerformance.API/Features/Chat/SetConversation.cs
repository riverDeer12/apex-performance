using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Hubs;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

public record SetConversationRequest(Guid ClientId, bool IsEnabled);

/// <summary>
/// Coach turns the chat with one of their clients on or off. Turning it off
/// keeps the messages, the client just can't see or use the chat any more.
/// </summary>
public class SetConversationEndpoint : Endpoint<SetConversationRequest, ChatConversationResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHubContext<ChatHub> _hub;

    public SetConversationEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IHubContext<ChatHub> hub)
    {
        _context = context;
        _currentUserService = currentUserService;
        _hub = hub;
    }

    public override void Configure()
    {
        Post("api/chat/conversations");
        Roles(UserRoles.Coach);
        Options(x => x.WithTags("Chat"));
    }

    public override async Task HandleAsync(SetConversationRequest request, CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches
            .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId, cancellationToken);

        if (coach is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.Id == request.ClientId &&
                                      _context.CoachClients.Any(coachClient =>
                                          coachClient.CoachId == coach.Id && coachClient.ClientId == x.Id),
                cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(x => x.CoachId == coach.Id && x.ClientId == client.Id, cancellationToken);

        if (conversation is null)
        {
            conversation = new ChatConversation { CoachId = coach.Id, ClientId = client.Id };
            _context.ChatConversations.Add(conversation);
        }

        conversation.IsEnabled = request.IsEnabled;

        await _context.SaveChangesAsync(cancellationToken);

        await _hub.Clients.Users(coach.UserId.ToString(), client.UserId.ToString())
            .SendAsync(ChatHub.Events.ConversationUpdated, conversation.Id, cancellationToken);

        await SendAsync(new ChatConversationResponse(conversation.Id, ChatAccess.Participant(coach),
                ChatAccess.Participant(client), conversation.IsEnabled, conversation.LastMessageAt, null, 0),
            cancellation: cancellationToken);
    }
}
