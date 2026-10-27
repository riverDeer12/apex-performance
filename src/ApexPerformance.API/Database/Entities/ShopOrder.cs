using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public static class ShopOrderStatuses
{
    // Created, waiting for the payment in Stripe Checkout.
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Shipped = "Shipped";
    // Not paid in time, or cancelled by the shop.
    public const string Cancelled = "Cancelled";
    // All the money returned in Stripe.
    public const string Refunded = "Refunded";

    public static readonly string[] All = [Pending, Paid, Shipped, Cancelled, Refunded];
}

/// <summary>
/// Order from the webshop. Paid by card in Stripe Checkout and delivered
/// to a BoxNow locker. The fiscalized receipt is sent to the customer by
/// eRačuni, which reads the payments from Stripe.
/// </summary>
public class ShopOrder : BaseEntity
{
    // Number shown to the customer and on the BoxNow parcel, e.g. "AP-261027-4821".
    public required string Number { get; set; }

    public required string Status { get; set; }

    // Filled from Stripe Checkout once paid.
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    // Before the promo code discount.
    public decimal Subtotal { get; set; }

    public string? PromoCode { get; set; }
    public decimal Discount { get; set; }

    public decimal ShippingPrice { get; set; }
    public decimal Total { get; set; }

    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public required string BoxNowLockerId { get; set; }
    public required string BoxNowLockerAddress { get; set; }
    public required string BoxNowLockerPostalCode { get; set; }

    // Parcel created at BoxNow after the payment.
    public string? BoxNowParcelId { get; set; }

    // Stock was lower than ordered when the order was paid, the shop has to check it.
    public bool HasStockIssue { get; set; }

    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }

    // Returned in Stripe, all or part of the total (from the charge.refunded webhook).
    public decimal RefundedAmount { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }

    // Stripe status of a dispute (chargeback) of the payment, e.g. "needs_response", "won", "lost".
    public string? DisputeStatus { get; set; }
    public DateTimeOffset? DisputedAt { get; set; }

    // The card's bank reported the payment as likely fraud, don't ship before checking.
    public bool HasFraudWarning { get; set; }

    public ICollection<ShopOrderItem> Items { get; set; } = new List<ShopOrderItem>();
}
