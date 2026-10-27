using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

/// <summary>
/// Checks promo codes and works out their discount. Used both when the
/// customer applies a code in the cart and again at checkout.
/// </summary>
internal static class ShopPromo
{
    public static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public static IQueryable<ShopOrder> Uses(ApexPerformanceContext context, string code)
        => context.ShopOrders.Where(x => x.PromoCode == code &&
                                         (x.Status == ShopOrderStatuses.Paid || x.Status == ShopOrderStatuses.Shipped));

    /// <summary>
    /// The usable code, or the error code to show: unknown, inactive, outside its
    /// dates and used up all look the same to the customer.
    /// </summary>
    public static async Task<(ShopPromoCode? Promo, string? Error)> Find(ApexPerformanceContext context, string code,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(code);
        var now = DateTimeOffset.UtcNow;

        var promo = await context.ShopPromoCodes
            .AsNoTracking()
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Code == normalized && !x.IsDeleted && x.IsActive, cancellationToken);

        if (promo is null || promo.ValidFrom > now || promo.ValidUntil < now)
            return (null, ErrorCodes.PromoCodeInvalid);

        if (promo.MaxUses is { } maxUses && await Uses(context, promo.Code).CountAsync(cancellationToken) >= maxUses)
            return (null, ErrorCodes.PromoCodeInvalid);

        return (promo, null);
    }

    // Discount on one piece of the product, 0 when the code is not for it.
    public static decimal UnitDiscount(ShopPromoCode promo, Guid productId, decimal price)
    {
        if (promo.Products.Count > 0 && promo.Products.All(x => x.ProductId != productId))
            return 0;

        var discount = promo.DiscountType == ShopDiscountTypes.Percent
            ? Math.Round(price * promo.DiscountValue / 100, 2, MidpointRounding.AwayFromZero)
            : promo.DiscountValue;

        return Math.Min(discount, price);
    }
}
