using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Hubs;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

public record StartConversationRequest(Guid CoachId);

/// <summary>
/// Client opens the chat with one of their coaches. Returns the existing
/// conversation when there is one, also when the coach disabled it (then
/// it's read only and the client can't turn it back on).
/// </summary>
public class StartConversationEndpoint : Endpoint<StartConversationRequest, ChatConversationResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHubContext<ChatHub> _hub;

    public StartConversationEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IHubContext<ChatHub> hub)
    {
        _context = context;
        _currentUserService = currentUserService;
        _hub = hub;
    }

    public override void Configure()
    {
        Post("api/chat/conversations/start");
        Roles(UserRoles.Client);
        Options(x => x.WithTags("Chat"));
    }

    public override async Task HandleAsync(StartConversationRequest request, CancellationToken cancellationToken)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId, cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var coach = await _context.Coaches
            .FirstOrDefaultAsync(x => x.Id == request.CoachId &&
                                      _context.CoachClients.Any(coachClient =>
                                          coachClient.CoachId == x.Id && coachClient.ClientId == client.Id),
                cancellationToken);

        if (coach is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var conversation = await FindConversation(coach.Id, client.Id, cancellationToken);

        if (conversation is null)
        {
            _context.ChatConversations.Add(new ChatConversation
                { CoachId = coach.Id, ClientId = client.Id, IsEnabled = true });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Started at the same time from another device, the unique
                // index keeps one conversation per coach and client.
                _context.ChangeTracker.Clear();
            }

            conversation = await FindConversation(coach.Id, client.Id, cancellationToken);

            if (conversation is null)
                ThrowError(ErrorCodes.SavingError);

            await _hub.Clients.Users(coach.UserId.ToString(), client.UserId.ToString())
                .SendAsync(ChatHub.Events.ConversationUpdated, conversation.Id, cancellationToken);
        }

        await SendAsync(ChatAccess.ToResponse(conversation, coach, client), cancellation: cancellationToken);
    }

    private Task<ChatConversation?> FindConversation(Guid coachId, Guid clientId, CancellationToken cancellationToken)
        => _context.ChatConversations
            .FirstOrDefaultAsync(x => x.CoachId == coachId && x.ClientId == clientId, cancellationToken);
}
