using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Chat between a coach and one of their clients. Either side can write
/// while the coach has it enabled and the client hasn't blocked it.
/// </summary>
public class ChatConversation : BaseEntity
{
    public Guid CoachId { get; set; }
    public Coach Coach { get; set; } = null!;

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    // Coach's setting.
    public bool IsEnabled { get; set; }

    // The client blocked the chat (App Store rule for user content): read
    // only for both until the client unblocks it, whatever the coach sets.
    public bool IsBlockedByClient { get; set; }

    // Time of the newest message, conversations are sorted by it.
    public DateTimeOffset? LastMessageAt { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
