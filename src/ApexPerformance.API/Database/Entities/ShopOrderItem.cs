using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Ordered variant with the name and price at the time of the order,
/// so later changes of the product don't change past orders.
/// </summary>
public class ShopOrderItem
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public ShopOrder Order { get; set; } = null!;

    public Guid VariantId { get; set; }
    public ShopProductVariant Variant { get; set; } = null!;

    public required string ProductName { get; set; }

    // Size and color, e.g. "M / Crna", empty for products without variants.
    public string? VariantName { get; set; }

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
