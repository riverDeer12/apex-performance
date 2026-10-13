namespace ApexPerformance.API.Features.Chat;

public record ChatParticipantResponse(Guid Id, Guid UserId, string FullName);

public record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    // Null when the message can't be decrypted.
    string? Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt
);

public record ChatConversationResponse(
    Guid Id,
    ChatParticipantResponse Coach,
    ChatParticipantResponse Client,
    bool IsEnabled,
    DateTimeOffset? LastMessageAt,
    ChatMessageResponse? LastMessage,
    // Messages of the other side the logged user hasn't read yet.
    int UnreadCount
);

public record MessagesReadEvent(Guid ConversationId, Guid ReaderUserId, DateTimeOffset ReadAt);

// Coach or client the logged user can chat with.
public record ChatContactResponse(Guid Id, Guid UserId, string FullName, Guid? ConversationId, bool IsEnabled);
