using System.ComponentModel.DataAnnotations;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Settings of the webshop changed in the administration. One row; empty
/// values fall back to the server settings (see ShopSettings).
/// </summary>
public class ShopConfig
{
    public const int SingleId = 1;

    [Key]
    public int Id { get; set; } = SingleId;

    // BoxNow delivery price and the products total from which it is free.
    public decimal? ShippingPrice { get; set; }
    public decimal? FreeShippingFrom { get; set; }

    // Percent off every product for clients of the studio logged in to the shop, 0 for none.
    public decimal ClientDiscountPercent { get; set; }

    // New orders, BoxNow labels and payment alerts are sent here.
    public string? NotificationEmail { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
