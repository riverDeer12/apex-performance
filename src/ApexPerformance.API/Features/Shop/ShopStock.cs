using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Features.Shop;

internal static class ShopStock
{
    /// <summary>
    /// Records a change already made to the variant's stock. Saved together
    /// with the change by the caller's SaveChanges.
    /// </summary>
    public static void Record(ApexPerformanceContext context, ShopProductVariant variant, int change, string reason,
        string? note = null, Guid? orderId = null, Guid? userId = null)
    {
        if (change == 0)
            return;

        context.ShopStockMovements.Add(new ShopStockMovement
        {
            Variant = variant,
            Change = change,
            StockAfter = variant.Stock,
            Reason = reason,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            OrderId = orderId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId is { } id && id != Guid.Empty ? id : null
        });
    }
}
