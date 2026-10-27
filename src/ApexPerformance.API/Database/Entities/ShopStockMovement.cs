using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

public static class ShopStockReasons
{
    // Stock of a new variant.
    public const string Initial = "Initial";
    // Goods received.
    public const string Receipt = "Receipt";
    // Counted stock differs, or changed in the product form.
    public const string Correction = "Correction";
    // Damaged or lost pieces.
    public const string WriteOff = "WriteOff";
    public const string Sale = "Sale";
    // Pieces of a cancelled paid order back in stock.
    public const string Return = "Return";

    public static readonly string[] Manual = [Receipt, Correction, WriteOff];
}

/// <summary>
/// One change of the stock of a variant, so the warehouse shows where every
/// piece went. Never changed or deleted.
/// </summary>
public class ShopStockMovement
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid VariantId { get; set; }
    public ShopProductVariant Variant { get; set; } = null!;

    // Pieces added (positive) or taken (negative).
    public int Change { get; set; }
    public int StockAfter { get; set; }

    public required string Reason { get; set; }
    public string? Note { get; set; }

    // Order of a sale or a return.
    public Guid? OrderId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Administrator who made the change, empty for sales.
    public Guid? CreatedBy { get; set; }
}
