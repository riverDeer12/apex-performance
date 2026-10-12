using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Chat between a coach and one of their clients. The coach turns it on,
/// the client sees and uses it only while it is enabled.
/// </summary>
public class ChatConversation : BaseEntity
{
    public Guid CoachId { get; set; }
    public Coach Coach { get; set; } = null!;

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public bool IsEnabled { get; set; }

    // Time of the newest message, conversations are sorted by it.
    public DateTimeOffset? LastMessageAt { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
