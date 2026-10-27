using System.ComponentModel.DataAnnotations;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Stripe webhook event already handled. Stripe can send an event more
/// than once, the second time it is only acknowledged.
/// </summary>
public class ShopStripeEvent
{
    // Stripe's event id, e.g. "evt_1Q...".
    [Key]
    public required string Id { get; set; }

    public required string Type { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }
}
