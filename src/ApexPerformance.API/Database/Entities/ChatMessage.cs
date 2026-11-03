using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Message of a chat. The content is stored encrypted (AES-256-GCM),
/// KeyId says which key it was encrypted with, so keys can be replaced
/// without losing older messages.
/// </summary>
public class ChatMessage
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;

    public Guid SenderUserId { get; set; }

    public ChatMessageType Type { get; set; }

    // Text, or the caption of a picture (empty when there is none).
    public required string EncryptedContent { get; set; }
    public required string KeyId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // When the other side of the conversation read the message.
    public DateTimeOffset? ReadAt { get; set; }

    // Picture of an Image message (ChatAttachment), with what the
    // apps need to lay it out before it is loaded.
    public Guid? AttachmentId { get; set; }
    public string? AttachmentContentType { get; set; }
    public int? AttachmentWidth { get; set; }
    public int? AttachmentHeight { get; set; }

    // Workout a coach shared (Workout message).
    public Guid? WorkoutId { get; set; }
}

public enum ChatMessageType
{
    Text = 0,
    Image = 1,
    Workout = 2
}
