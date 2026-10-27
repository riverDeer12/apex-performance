using System.Globalization;
using System.Text;
using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Features.Shop;

/// <summary>
/// Settings of the webshop. Everything has a default, so the shop works on
/// test without extra settings; production sets its own in the server settings.
/// </summary>
public class ShopSettings
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public ShopSettings(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    private bool IsProduction => _environment.IsProduction();

    // Address of the shop, the customer returns there from Stripe Checkout.
    public string ShopUrl => (_configuration["Shop:Url"] ??
                              (IsProduction ? "https://shop.apex-performance.fit" :
                                  _environment.IsDevelopment() ? "http://localhost:4200" :
                                  "https://test.shop.apex-performance.fit")).TrimEnd('/');

    // BoxNow delivery, free from FreeShippingFrom on.
    public decimal ShippingPrice => _configuration.GetValue<decimal?>("Shop:ShippingPrice") ?? 3.50m;
    public decimal FreeShippingFrom => _configuration.GetValue<decimal?>("Shop:FreeShippingFrom") ?? 50m;

    // New orders and BoxNow labels are sent here.
    public string NotificationEmail => _configuration["Shop:NotificationEmail"] ??
                                       (IsProduction ? "gabriel@apex-performance.fit" : "info@rdd.software");

    public string? StripeSecretKey => _configuration["Stripe:ApexPerformance:SecretKey"];

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
