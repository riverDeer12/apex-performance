using ApexPerformance.API.Features.Workouts;

namespace ApexPerformance.API.Features.Chat;

public record ChatParticipantResponse(Guid Id, Guid UserId, string FullName);

public record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    // Null when the message can't be decrypted.
    string? Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    // "text", "image" or "workout".
    string Type,
    ChatAttachmentResponse? Attachment,
    // Whole workout (as GET workouts returns it), so it opens even when the
    // client's plan doesn't list it. Null when the workout was deleted.
    GetWorkoutResponse? Workout
);

// Picture of an image message, loaded from GET .../attachments/{id}.
public record ChatAttachmentResponse(Guid Id, string ContentType, int? Width, int? Height);


public record ChatConversationResponse(
    Guid Id,
    ChatParticipantResponse Coach,
    ChatParticipantResponse Client,
    // Whether anyone can write: the coach enabled it and the client hasn't blocked it.
    bool IsEnabled,
    DateTimeOffset? LastMessageAt,
    ChatMessageResponse? LastMessage,
    // Messages of the other side the logged user hasn't read yet.
    int UnreadCount,
    bool IsBlockedByClient
);

public record MessagesReadEvent(Guid ConversationId, Guid ReaderUserId, DateTimeOffset ReadAt);

// Coach or client the logged user can chat with.
public record ChatContactResponse(Guid Id, Guid UserId, string FullName, Guid? ConversationId, bool IsEnabled,
    bool IsBlockedByClient);
