using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public record ShopVisitRequest(string Path);

public record ShopStatsDayResponse(DateOnly Date, int Visits, int ProductViews, int Orders, decimal Revenue);

public record ShopStatsProductResponse(string Slug, string Name, int Views, int SoldPieces);

public record ShopStatsResponse(
    int Days,
    int Visits,
    int ProductViews,
    int CartViews,
    int Orders,
    decimal Revenue,
    List<ShopStatsDayResponse> PerDay,
    List<ShopStatsProductResponse> Products);

internal static partial class ShopVisitPaths
{
    private static readonly string[] Pages =
        ["/", "/kosarica", "/prijava", "/moje-narudzbe", "/uvjeti-kupnje", "/povrat-i-reklamacije", "/privatnost"];

    /// <summary>
    /// Path to count, or null for one that is not counted. Order pages
    /// are counted together, without the order's id.
    /// </summary>
    public static string? Normalize(string path)
    {
        path = path.Split('?', '#')[0].TrimEnd('/');
        if (path.Length == 0) path = "/";

        if (path.StartsWith("/narudzba/", StringComparison.Ordinal)) return "/narudzba";
        if (Pages.Contains(path)) return path;

        var product = ProductPath().Match(path);
        return product.Success ? path : null;
    }

    [GeneratedRegex("^/proizvod/[a-z0-9-]{1,200}$")]
    private static partial Regex ProductPath();
}

/// <summary>
/// Counts a page view of the shop. Only the page and the day are kept, nothing
/// about the visitor, so the shop needs no cookies or consent for it.
/// </summary>
public class CreateShopVisitEndpoint : Endpoint<ShopVisitRequest>
{
    private readonly ApexPerformanceContext _context;

    public CreateShopVisitEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/shop/visits");
        AllowAnonymous();
        Options(x => x.WithTags("Shop").RequireRateLimiting(RateLimiting.ShopVisits));
    }

    public override async Task HandleAsync(ShopVisitRequest request, CancellationToken cancellationToken)
    {
        var path = ShopVisitPaths.Normalize(request.Path ?? "");

        if (path is not null)
        {
            // Croatian day, the shop's visitors are there.
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CroatianTime.Zone).Date);

            var updated = await _context.ShopVisitDays
                .Where(x => x.Date == today && x.Path == path)
                .ExecuteUpdateAsync(x => x.SetProperty(v => v.Count, v => v.Count + 1), cancellationToken);

            if (updated == 0)
            {
                try
                {
                    _context.ShopVisitDays.Add(new ShopVisitDay { Date = today, Path = path, Count = 1 });
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Another view of the same page added the day's row first.
                    await _context.ShopVisitDays
                        .Where(x => x.Date == today && x.Path == path)
                        .ExecuteUpdateAsync(x => x.SetProperty(v => v.Count, v => v.Count + 1), cancellationToken);
                }
            }
        }

        await SendNoContentAsync(cancellationToken);
    }
}

/// <summary>
/// Visits and sales of the shop in the last days, for the administration.
/// </summary>
public class GetShopStatsEndpoint : EndpointWithoutRequest<ShopStatsResponse>
{
    private readonly ApexPerformanceContext _context;

    public GetShopStatsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/shop/admin/stats");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var days = Math.Clamp(Query<int?>("days", isRequired: false) ?? 30, 1, 365);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CroatianTime.Zone).Date);
        var from = today.AddDays(1 - days);
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);

        var visits = await _context.ShopVisitDays
            .AsNoTracking()
            .Where(x => x.Date >= from)
            .ToListAsync(cancellationToken);

        // Orders that were paid (refunded and cancelled ones don't count as sales).
        var orders = await _context.ShopOrders
            .AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.Variant).ThenInclude(x => x.Product)
            .Where(x => x.PaidAt != null && x.PaidAt >= fromUtc &&
                        (x.Status == ShopOrderStatuses.Paid || x.Status == ShopOrderStatuses.Shipped))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var paid = orders
            .Select(x => (Order: x,
                Day: DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.PaidAt!.Value, CroatianTime.Zone).Date)))
            .Where(x => x.Day >= from)
            .ToList();

        var perDay = Enumerable.Range(0, days)
            .Select(offset => from.AddDays(offset))
            .Select(day => new ShopStatsDayResponse(day,
                visits.Where(x => x.Date == day).Sum(x => x.Count),
                visits.Where(x => x.Date == day && x.Path.StartsWith("/proizvod/")).Sum(x => x.Count),
                paid.Count(x => x.Day == day),
                paid.Where(x => x.Day == day).Sum(x => x.Order.Total)))
            .ToList();

        var products = await _context.ShopProducts
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new { x.Id, x.Slug, x.Name })
            .ToListAsync(cancellationToken);

        var productStats = products
            .Select(product => new ShopStatsProductResponse(product.Slug, product.Name,
                visits.Where(x => x.Path == "/proizvod/" + product.Slug).Sum(x => x.Count),
                paid.SelectMany(x => x.Order.Items).Where(x => x.Variant.ProductId == product.Id).Sum(x => x.Quantity)))
            .Where(x => x.Views > 0 || x.SoldPieces > 0)
            .OrderByDescending(x => x.Views)
            .ThenByDescending(x => x.SoldPieces)
            .ToList();

        await SendAsync(new ShopStatsResponse(days,
                visits.Sum(x => x.Count),
                visits.Where(x => x.Path.StartsWith("/proizvod/")).Sum(x => x.Count),
                visits.Where(x => x.Path == "/kosarica").Sum(x => x.Count),
                paid.Count,
                paid.Sum(x => x.Order.Total),
                perDay,
                productStats),
            cancellation: cancellationToken);
    }
}

/// <summary>
/// Page of a product for sharing (Instagram, WhatsApp, Facebook): link
/// previews read its title, description and picture, and a person opening
/// it goes on to the product in the shop. The shop runs in the browser,
/// so these apps can't read the product from the shop itself.
/// </summary>
public class GetShopSharePageEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ShopSettings _settings;

    public GetShopSharePageEndpoint(ApexPerformanceContext context, ShopSettings settings)
    {
        _context = context;
        _settings = settings;
    }

    public override void Configure()
    {
        Get("api/shop/share/{slug}");
        AllowAnonymous();
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var slug = Route<string>("slug", isRequired: true)!;

        var product = await _context.ShopProducts
            .AsNoTracking()
            .Where(x => x.Slug == slug && x.IsActive && !x.IsDeleted)
            .Select(x => new
            {
                x.Name,
                x.Description,
                x.Price,
                ImageId = x.Images.OrderBy(i => i.SortOrder).Select(i => (Guid?)i.Id).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var shopUrl = _settings.ShopUrl;
        var url = product is null ? shopUrl : $"{shopUrl}/proizvod/{slug}";
        var apiUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

        string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var title = product is null ? "Apex Performance Shop" : $"{product.Name} · {ShopSettings.FormatPrice(product.Price)}";
        var description = product is null
            ? "Odjeća i oprema studija Apex Performance."
            : product.Description.Length > 200 ? product.Description[..197] + "…" : product.Description;
        var image = product?.ImageId is { } imageId
            ? $"{apiUrl}/api/shop/images/{imageId}"
            : $"{apiUrl}/assets/images/logo_light_theme.png";

        var html = new StringBuilder()
            .Append("<!doctype html><html lang=\"hr\"><head><meta charset=\"utf-8\">")
            .Append($"<title>{E(title)}</title>")
            .Append($"<meta name=\"description\" content=\"{E(description)}\">")
            .Append("<meta property=\"og:type\" content=\"product\">")
            .Append("<meta property=\"og:site_name\" content=\"Apex Performance Shop\">")
            .Append($"<meta property=\"og:title\" content=\"{E(title)}\">")
            .Append($"<meta property=\"og:description\" content=\"{E(description)}\">")
            .Append($"<meta property=\"og:image\" content=\"{E(image)}\">")
            .Append($"<meta property=\"og:url\" content=\"{E(url)}\">")
            .Append("<meta name=\"twitter:card\" content=\"summary_large_image\">")
            .Append($"<link rel=\"canonical\" href=\"{E(url)}\">")
            .Append($"<meta http-equiv=\"refresh\" content=\"0;url={E(url)}\">")
            .Append("<meta name=\"robots\" content=\"noindex\">")
            .Append("</head><body>")
            .Append($"<p><a href=\"{E(url)}\">{E(title)}</a></p>")
            .Append($"<script>location.replace({System.Text.Json.JsonSerializer.Serialize(url)});</script>")
            .Append("</body></html>")
            .ToString();

        HttpContext.Response.Headers.CacheControl = "public, max-age=600";
        await SendStringAsync(html, contentType: "text/html; charset=utf-8", cancellation: cancellationToken);
    }
}

/// <summary>
/// Sitemap of the shop for search engines, listed in the shop's robots.txt.
/// </summary>
public class GetShopSitemapEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ShopSettings _settings;

    public GetShopSitemapEndpoint(ApexPerformanceContext context, ShopSettings settings)
    {
        _context = context;
        _settings = settings;
    }

    public override void Configure()
    {
        Get("api/shop/sitemap.xml");
        AllowAnonymous();
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var products = await _context.ShopProducts
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .Select(x => new { x.Slug, x.UpdatedAt })
            .ToListAsync(cancellationToken);

        var shopUrl = _settings.ShopUrl;
        var xml = new StringBuilder()
            .Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
            .Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">")
            .Append($"<url><loc>{shopUrl}/</loc></url>");

        foreach (var product in products)
            xml.Append($"<url><loc>{shopUrl}/proizvod/{WebUtility.HtmlEncode(product.Slug)}</loc>" +
                       $"<lastmod>{product.UpdatedAt:yyyy-MM-dd}</lastmod></url>");

        foreach (var page in new[] { "uvjeti-kupnje", "povrat-i-reklamacije", "privatnost" })
            xml.Append($"<url><loc>{shopUrl}/{page}</loc></url>");

        xml.Append("</urlset>");

        HttpContext.Response.Headers.CacheControl = "public, max-age=3600";
        await SendStringAsync(xml.ToString(), contentType: "application/xml; charset=utf-8",
            cancellation: cancellationToken);
    }
}

internal static class CroatianTime
{
    // IANA id on Linux and newer Windows, Windows id as fallback, like the background jobs.
    public static readonly TimeZoneInfo Zone =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Zagreb", out var zone) ? zone
        : TimeZoneInfo.TryFindSystemTimeZoneById("Central European Standard Time", out zone) ? zone
        : TimeZoneInfo.Utc;
}
