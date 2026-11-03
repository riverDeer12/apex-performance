using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Size and color of a shop product with its stock. A product without
/// sizes or colors has one variant with both empty.
/// </summary>
public class ShopProductVariant
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public ShopProduct Product { get; set; } = null!;

    public string? Size { get; set; }
    public string? Color { get; set; }

    // Pieces in stock, lowered when an order is paid.
    public int Stock { get; set; }

    public int SortOrder { get; set; }

    // Removed variants are kept for the orders that have them.
    public bool IsDeleted { get; set; }
}
