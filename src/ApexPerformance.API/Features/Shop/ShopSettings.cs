using System.Globalization;
using System.Text;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

/// <summary>
/// Settings of the webshop: the ones changed in the administration (ShopConfig),
/// then the server settings, then defaults, so the shop works on test without
/// extra settings. One per request, the administration's row is read once.
/// </summary>
public class ShopSettings
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ApexPerformanceContext _context;
    private ShopConfig? _config;

    public ShopSettings(IConfiguration configuration, IHostEnvironment environment, ApexPerformanceContext context)
    {
        _configuration = configuration;
        _environment = environment;
        _context = context;
    }

    // Row of the administration, an empty one until it is first saved.
    public ShopConfig Config => _config ??=
        _context.ShopConfigs.AsNoTracking().FirstOrDefault() ?? new ShopConfig();

    public decimal ClientDiscountPercent => Config.ClientDiscountPercent;

    private bool IsProduction => _environment.IsProduction();

    // Address of the shop, the customer returns there from Stripe Checkout.
    public string ShopUrl => (_configuration["Shop:Url"] ??
                              (IsProduction ? "https://shop.apex-performance.fit" :
                                  _environment.IsDevelopment() ? "http://localhost:4200" :
                                  "https://test.shop.apex-performance.fit")).TrimEnd('/');

    // BoxNow delivery, free from FreeShippingFrom on.
    public decimal ShippingPrice => Config.ShippingPrice ?? DefaultShippingPrice;
    public decimal FreeShippingFrom => Config.FreeShippingFrom ?? DefaultFreeShippingFrom;

    public decimal DefaultShippingPrice => _configuration.GetValue<decimal?>("Shop:ShippingPrice") ?? 3.50m;
    public decimal DefaultFreeShippingFrom => _configuration.GetValue<decimal?>("Shop:FreeShippingFrom") ?? 50m;

    // New orders and BoxNow labels are sent here.
    public string NotificationEmail => Config.NotificationEmail ?? DefaultNotificationEmail;

    public string DefaultNotificationEmail => _configuration["Shop:NotificationEmail"] ??
                                              (IsProduction ? "gabriel@apex-performance.fit" : "info@rdd.software");

    public string? StripeSecretKey => _configuration["Stripe:ApexPerformance:SecretKey"];

    // Signing secret of the webhook endpoint added in the Stripe dashboard ("whsec_...").
    public string? StripeWebhookSecret => _configuration["Stripe:ApexPerformance:WebhookSecret"];

    /// <summary>
    /// BoxNow settings of Apex Performance. Until it has its own, test and
    /// development use the ReViv Plus ones (the test BoxNow API is the same
    /// for everyone); production never borrows them.
    /// </summary>
    public string? BoxNow(string key)
        => _configuration[$"BoxNow:ApexPerformance:{key}"] ??
           (IsProduction ? null : _configuration[$"BoxNow:ReVivPlus:{key}"]);

    // Public id for the BoxNow locker map in the shop.
    public string? BoxNowPartnerId => BoxNow("PartnerId") ?? (IsProduction ? null : "11406");

    public decimal ShippingFor(decimal subtotal) => subtotal >= FreeShippingFrom ? 0 : ShippingPrice;

    public static string VariantName(ShopProductVariant variant)
    {
        var name = string.Join(" / ", new[] { variant.Size, variant.Color }.Where(x => !string.IsNullOrWhiteSpace(x)));

        return name.Length == 0 ? string.Empty : name;
    }

    // "Majica Apex – crna!" -> "majica-apex-crna".
    public static string ToSlug(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace("đ", "d")
            .Normalize(NormalizationForm.FormD);

        var slug = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                slug.Append(c);
            else if (slug.Length > 0 && slug[^1] != '-')
                slug.Append('-');
        }

        return slug.ToString().Trim('-');
    }

    public static string FormatPrice(decimal value) => value.ToString("0.00", new CultureInfo("hr-HR")) + " €";
}
