using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Picture (photo or GIF) sent in a chat, stored encrypted like message
/// content. The message that shows it keeps its id, type and size.
/// </summary>
public class ChatAttachment
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;

    public required string ContentType { get; set; }

    public required byte[] EncryptedData { get; set; }
    public required string KeyId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
