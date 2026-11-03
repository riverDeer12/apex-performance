using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// One change of an entity (created, updated, deleted or restored),
/// written automatically when changes are saved.
/// </summary>
public class AuditLog
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    // Null for changes made by background jobs or anonymous requests.
    public Guid? UserId { get; set; }

    // One of AuditActions.
    public required string Action { get; set; }

    public required string EntityType { get; set; }
    public required string EntityId { get; set; }

    // Name of the entity at the time of the change, so it is recognizable later.
    public string? EntityName { get; set; }

    // JSON list of changed properties with old and new values.
    public string? Changes { get; set; }
}
