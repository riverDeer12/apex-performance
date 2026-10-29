using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Customer waiting for a sold out size or color: gets one email when it is
/// back in stock. Deleted after half a year, the email is not kept longer.
/// </summary>
public class ShopStockAlert
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid VariantId { get; set; }
    public ShopProductVariant Variant { get; set; } = null!;

    public required string Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? NotifiedAt { get; set; }
}
