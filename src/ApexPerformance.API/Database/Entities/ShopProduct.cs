using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Product of the webshop (shop.apex-performance.fit). What is bought is
/// one of its variants (size and color), each with its own stock.
/// </summary>
public class ShopProduct : BaseEntity
{
    public required string Name { get; set; }

    // Part of the product's address in the shop, e.g. "majica-apex-crna".
    public required string Slug { get; set; }

    public required string Description { get; set; }

    // Free text the shop groups products by, e.g. "Majice".
    public required string Category { get; set; }

    public decimal Price { get; set; }

    // Only active products are shown in the shop.
    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ShopProductVariant> Variants { get; set; } = new List<ShopProductVariant>();

    public ICollection<ShopProductImage> Images { get; set; } = new List<ShopProductImage>();
}
