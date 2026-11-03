using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Web shop product listed in the daily price list required by law.
/// Current price is read from Stripe, where the shop owner changes it,
/// anchor price is fixed (price on the anchor date) and kept here so it
/// can not be lost while editing the product in Stripe.
/// </summary>
public class PriceListProduct : BaseEntity
{
    public required string StripeProductId { get; set; }
    public required string Code { get; set; }
    public required string Brand { get; set; }
    public decimal NetQuantity { get; set; }
    public required string UnitOfMeasure { get; set; }
    public string? Barcode { get; set; }
    public required string Category { get; set; }
    public decimal AnchorPrice { get; set; }
    public int SortOrder { get; set; }
}
