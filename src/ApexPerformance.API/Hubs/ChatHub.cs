using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ApexPerformance.API.Hubs;

/// <summary>
/// Real-time delivery of chat events. Clients only listen, messages are sent
/// through the REST API, which then notifies both sides of the conversation:
/// "MessageReceived" (ChatMessageResponse), "MessagesRead" (MessagesReadEvent)
/// and "ConversationUpdated" (conversation id).
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    public const string Path = "/api/hubs/chat";

    public static class Events
    {
        public const string MessageReceived = "MessageReceived";
        public const string MessagesRead = "MessagesRead";
        public const string ConversationUpdated = "ConversationUpdated";
    }
}

/// <summary>
/// SignalR user id is the user id from the "sub" claim of the login token,
/// so events can be sent to all connections (devices) of a user.
/// </summary>
public class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User?.FindFirst("sub")?.Value;
}
