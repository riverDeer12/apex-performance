using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Record of a client accepting a version of the cooperation rules: who, when,
/// which version and from where. Never changed or deleted, a new version is a
/// new record, so it stays a proof of what was accepted.
/// </summary>
public class RulesAcceptance
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    // User that accepted in the app (the client's account).
    public Guid UserId { get; set; }

    // RulesDocuments value and its version, e.g. "individual-coaching" 1.1.
    public required string Document { get; set; }
    public required string Version { get; set; }

    public DateTimeOffset AcceptedAt { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Parent or guardian who accepted for a minor client.
    public string? GuardianName { get; set; }

    // Parent's written approval for a client aged 14-17 to come and leave alone.
    public bool IndependentTravelApproved { get; set; }
}
