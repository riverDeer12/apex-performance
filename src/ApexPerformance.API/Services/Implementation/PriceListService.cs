using System.Text.Json;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.PriceLists;
using ApexPerformance.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace ApexPerformance.API.Services.Implementation;

/// <summary>
/// Daily price list of the ReViv Plus web shop with current and anchor prices,
/// required for shops selling to consumers. Current prices are read from Stripe,
/// where the shop owner changes them.
/// </summary>
public class PriceListService : IPriceListService
{
    public const int RetentionDays = 30;

    private const string DefaultFileNamePrefix = "INTERNET_TRGOVINA_REVIVPLUS";
    private const string DefaultAnchorDate = "10.9.2026.";

    private readonly ApexPerformanceContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<PriceListService> _logger;

    public PriceListService(ApexPerformanceContext context, IConfiguration configuration,
        IEmailService emailService, ILogger<PriceListService> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    public string AnchorDate => _configuration["PriceList:ReVivPlus:AnchorDate"] ?? DefaultAnchorDate;

    private string FileNamePrefix =>
        _configuration["PriceList:ReVivPlus:FileNamePrefix"] ?? DefaultFileNamePrefix;

    private string? AlertEmail =>
        _configuration["PriceList:ReVivPlus:AlertEmail"] ?? _configuration["BoxNow:ReVivPlus:ContactEmail"];

    public async Task<PriceListFile> PublishAsync(string reason)
    {
        var products = await _context.PriceListProducts
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        if (products.Count == 0)
            throw new InvalidOperationException("No products are set up for the price list.");

        var currentPrices = await GetStripePricesAsync(products);

        var now = DateTimeOffset.UtcNow;
        var localNow = TimeZoneInfo.ConvertTime(now, GetCroatianTimeZone());

        var lowestPrices = await GetLowestPricesAsync(now, currentPrices);

        var rows = products.Select(product =>
        {
            var (name, price) = currentPrices[product.StripeProductId];

            return new PriceListRow(name, product.Code, product.Brand, product.NetQuantity,
                product.UnitOfMeasure, price, null, lowestPrices[product.StripeProductId],
                product.AnchorPrice, product.Barcode, product.Category);
        });

        var fileName = PriceListCsv.BuildFileName(FileNamePrefix, localNow);

        // Two publishes in the same minute share the file name, the later one wins.
        var file = await _context.PriceListFiles.FirstOrDefaultAsync(x => x.FileName == fileName);

        if (file is null)
        {
            file = new PriceListFile
            {
                FileName = fileName,
                Content = "",
                Reason = reason,
                PricesJson = ""
            };

            _context.PriceListFiles.Add(file);
        }

        file.Content = PriceListCsv.Build(rows, AnchorDate);
        file.PublishedAt = now;
        file.Reason = reason;
        file.PricesJson = JsonSerializer.Serialize(
            currentPrices.ToDictionary(x => x.Key, x => x.Value.Price));

        await RemoveExpiredFilesAsync(now);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Published price list {FileName} ({Reason}).", fileName, reason);

        return file;
    }

    public async Task PublishIfPricesChangedAsync()
    {
        var products = await _context.PriceListProducts.ToListAsync();

        if (products.Count == 0)
            return;

        var currentPrices = await GetStripePricesAsync(products);

        var lastFile = await _context.PriceListFiles
            .OrderByDescending(x => x.PublishedAt)
            .FirstOrDefaultAsync();

        var lastPrices = lastFile is null
            ? new Dictionary<string, decimal>()
            : ReadPrices(lastFile.PricesJson);

        var changed = currentPrices.Any(x =>
            !lastPrices.TryGetValue(x.Key, out var lastPrice) || lastPrice != x.Value.Price);

        if (changed)
            await PublishAsync(PriceListPublishReasons.PriceChange);
    }

    public async Task AlertIfNotPublishedTodayAsync()
    {
        var timeZone = GetCroatianTimeZone();
        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).Date;
        var startOfToday = new DateTimeOffset(today, timeZone.GetUtcOffset(today));

        var publishedToday = await _context.PriceListFiles
            .AnyAsync(x => x.PublishedAt >= startOfToday);

        if (publishedToday)
            return;

        _logger.LogError("Price list was not published today.");

        if (string.IsNullOrEmpty(AlertEmail))
            return;

        _emailService.SendPriceListAlertEmail(AlertEmail,
            $"Cjenik za reviv-plus.com nije objavljen danas ({today:d.M.yyyy.}). " +
            "Objava je obavezna do 8:00. Provjerite Hangfire posao \"reviv-plus-price-list\" " +
            "i ručno objavite cjenik.");
    }

    public async Task<Dictionary<string, decimal>> GetAnchorPricesAsync(
        CancellationToken cancellationToken = default)
        => await _context.PriceListProducts
            .ToDictionaryAsync(x => x.StripeProductId, x => x.AnchorPrice, cancellationToken);

    private async Task<Dictionary<string, (string Name, decimal Price)>> GetStripePricesAsync(
        IEnumerable<PriceListProduct> products)
    {
        var requestOptions = new RequestOptions { ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"] };
        var productService = new ProductService();
        var priceService = new PriceService();

        var prices = new Dictionary<string, (string Name, decimal Price)>();

        foreach (var product in products)
        {
            var stripeProduct = await productService.GetAsync(product.StripeProductId,
                requestOptions: requestOptions);

            if (string.IsNullOrEmpty(stripeProduct.DefaultPriceId))
                throw new InvalidOperationException(
                    $"Product {product.StripeProductId} has no default price set.");

            var stripePrice = await priceService.GetAsync(stripeProduct.DefaultPriceId,
                requestOptions: requestOptions);

            prices[product.StripeProductId] = (stripeProduct.Name, (stripePrice.UnitAmountDecimal ?? 0) / 100);
        }

        return prices;
    }

    private async Task<Dictionary<string, decimal>> GetLowestPricesAsync(DateTimeOffset now,
        Dictionary<string, (string Name, decimal Price)> currentPrices)
    {
        var from = now.AddDays(-RetentionDays);

        var recentPrices = await _context.PriceListFiles
            .Where(x => x.PublishedAt >= from)
            .Select(x => x.PricesJson)
            .ToListAsync();

        var lowest = currentPrices.ToDictionary(x => x.Key, x => x.Value.Price);

        foreach (var prices in recentPrices.Select(ReadPrices))
        foreach (var (productId, price) in prices)
        {
            if (lowest.TryGetValue(productId, out var current) && price < current)
                lowest[productId] = price;
        }

        return lowest;
    }

    private async Task RemoveExpiredFilesAsync(DateTimeOffset now)
    {
        var expiredFiles = await _context.PriceListFiles
            .Where(x => x.PublishedAt < now.AddDays(-RetentionDays))
            .ToListAsync();

        _context.PriceListFiles.RemoveRange(expiredFiles);
    }

    private static Dictionary<string, decimal> ReadPrices(string pricesJson)
        => JsonSerializer.Deserialize<Dictionary<string, decimal>>(pricesJson) ?? new();

    public static TimeZoneInfo GetCroatianTimeZone()
    {
        // IANA id on Linux and newer Windows, Windows id as fallback.
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Zagreb", out var timeZone))
            return timeZone;

        return TimeZoneInfo.TryFindSystemTimeZoneById("Central European Standard Time", out timeZone)
            ? timeZone
            : TimeZoneInfo.Utc;
    }
}

public static class PriceListPublishReasons
{
    public const string Scheduled = "Dnevna objava";
    public const string PriceChange = "Promjena cijene";
    public const string Manual = "Ručna objava";
}
