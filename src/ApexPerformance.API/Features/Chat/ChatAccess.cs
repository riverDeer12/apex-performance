using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

/// <summary>
/// Only the coach and the client of a conversation can read it, nobody else
/// (administrators included), and only while the client is still one of the
/// coach's clients. A conversation the coach disabled stays visible to both
/// sides as read only.
/// </summary>
public static class ChatAccess
{
    public static IQueryable<ChatConversation> VisibleConversations(ApexPerformanceContext context, Guid userId)
        => context.ChatConversations
            .Where(x => x.Coach.UserId == userId || x.Client.UserId == userId)
            .Where(x => context.CoachClients.Any(coachClient =>
                coachClient.CoachId == x.CoachId && coachClient.ClientId == x.ClientId));

    public static async Task<ChatConversation?> GetConversation(ApexPerformanceContext context, Guid userId,
        Guid conversationId, CancellationToken cancellationToken)
        => await VisibleConversations(context, userId)
            .Include(x => x.Coach)
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);

    // Nobody can write while the coach has it disabled or the client blocked it.
    public static bool CanWrite(ChatConversation conversation)
        => conversation.IsEnabled && !conversation.IsBlockedByClient;

    // Response without the last message, after the conversation was created or changed.
    public static ChatConversationResponse ToResponse(ChatConversation conversation, Coach coach, Client client)
        => new(conversation.Id, Participant(coach), Participant(client), CanWrite(conversation),
            conversation.LastMessageAt, null, 0, conversation.IsBlockedByClient);

    public static ChatMessageResponse ToResponse(ChatMessage message, IChatEncryptionService encryption)
        => new(message.Id, message.ConversationId, message.SenderUserId,
            encryption.Decrypt(message.EncryptedContent, message.KeyId, message.ConversationId),
            message.CreatedAt, message.ReadAt);

    public static ChatParticipantResponse Participant(Coach coach) => new(coach.Id, coach.UserId, coach.FullName);

    public static ChatParticipantResponse Participant(Client client) => new(client.Id, client.UserId, client.FullName);
}
