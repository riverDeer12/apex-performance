using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public static class ShopDiscountTypes
{
    // Percent off the price of each eligible piece.
    public const string Percent = "Percent";
    // Euros off each eligible piece, at most its price.
    public const string Amount = "Amount";
}

/// <summary>
/// Code a customer enters in the cart for a discount on some products of the
/// shop, or on all of them when no product is chosen.
/// </summary>
public class ShopPromoCode : BaseEntity
{
    // Kept in capitals, customers can type it in any case.
    public required string Code { get; set; }

    public required string DiscountType { get; set; }
    public decimal DiscountValue { get; set; }

    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }

    // Paid orders that may use the code, unlimited when empty.
    public int? MaxUses { get; set; }

    public bool IsActive { get; set; }

    // For the administration only, e.g. "Instagram, studeni".
    public string? Note { get; set; }

    public ICollection<ShopPromoCodeProduct> Products { get; set; } = new List<ShopPromoCodeProduct>();
}

public class ShopPromoCodeProduct
{
    public Guid PromoCodeId { get; set; }
    public ShopPromoCode PromoCode { get; set; } = null!;

    public Guid ProductId { get; set; }
    public ShopProduct Product { get; set; } = null!;
}
