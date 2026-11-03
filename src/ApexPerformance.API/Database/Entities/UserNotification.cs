using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Push notification a user got, kept so the app can list them (an inbox)
/// when the push was missed or dismissed. Chat messages aren't kept, the
/// chat shows its own unread messages.
/// </summary>
public class UserNotification
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    // PushNotificationTypes value, the app opens the same screen as for the push.
    public required string Type { get; set; }

    // In the language of the user's app when it was sent.
    public required string Title { get; set; }
    public required string Body { get; set; }

    // The push data as JSON (type and ids like trainingId).
    public required string Data { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
