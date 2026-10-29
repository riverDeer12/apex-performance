using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public record ShopVariantResponse(
    Guid Id,
    string? Size,
    string? Color,
    bool InStock,
    // Pieces left when only a few are left, so the shop can say "last pieces".
    int? FewLeft);

public record ShopProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Category,
    decimal Price,
    // Picture ids, the first is the main picture: GET /api/shop/images/{id}.
    List<Guid> ImageIds,
    List<ShopVariantResponse> Variants,
    // For sorting the shop by newest.
    DateTimeOffset? CreatedAt = null);

public record ShopSettingsResponse(
    decimal ShippingPrice,
    decimal FreeShippingFrom,
    string? BoxNowPartnerId,
    // Shown to everyone, given to clients of the studio logged in to the shop.
    decimal ClientDiscountPercent = 0);

internal static class ShopCatalog
{
    private const int FewLeftLimit = 3;

    public static IQueryable<ShopProduct> Visible(ApexPerformanceContext context)
        => context.ShopProducts
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted && x.Variants.Any(v => !v.IsDeleted));

    public static ShopProductResponse ToResponse(ShopProduct product)
        => new(product.Id, product.Name, product.Slug, product.Description, product.Category, product.Price,
            product.Images.OrderBy(x => x.SortOrder).Select(x => x.Id).ToList(),
            product.Variants
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .Select(x => new ShopVariantResponse(x.Id, x.Size, x.Color, x.Stock > 0,
                    x.Stock is > 0 and <= FewLeftLimit ? x.Stock : null))
                .ToList(),
            product.CreatedAt);
}

/// <summary>
/// Loads products with their variants and the ids of their pictures,
/// without the picture data, which can be large.
/// </summary>
internal static class ShopProductLoader
{
    public static async Task<List<ShopProduct>> Load(IQueryable<ShopProduct> query, ApexPerformanceContext context,
        CancellationToken cancellationToken)
    {
        var products = await query
            .Include(x => x.Variants)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var ids = products.Select(x => x.Id).ToList();

        var images = await context.ShopProductImages
            .AsNoTracking()
            .Where(x => ids.Contains(x.ProductId))
            .Select(x => new { x.Id, x.ProductId, x.SortOrder, x.ContentType })
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.Images = images
                .Where(x => x.ProductId == product.Id)
                .Select(x => new ShopProductImage
                {
                    Id = x.Id, ProductId = x.ProductId, SortOrder = x.SortOrder, ContentType = x.ContentType,
                    Data = []
                })
                .ToList();
        }

        return products;
    }
}

/// <summary>
/// Active products of the shop, in the order set in the administration.
/// </summary>
public class GetShopProductsEndpoint : EndpointWithoutRequest<List<ShopProductResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetShopProductsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/shop/products");
        AllowAnonymous();
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var products = await ShopProductLoader.Load(ShopCatalog.Visible(_context), _context, cancellationToken);

        await SendAsync(products
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(ShopCatalog.ToResponse)
                .ToList(),
            cancellation: cancellationToken);
    }
}

/// <summary>
/// One active product by its slug, for the product page of the shop.
/// </summary>
public class GetShopProductEndpoint : EndpointWithoutRequest<ShopProductResponse>
{
    private readonly ApexPerformanceContext _context;

    public GetShopProductEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/shop/products/{slug}");
        AllowAnonymous();
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var slug = Route<string>("slug", isRequired: true)!;

        var products = await ShopProductLoader.Load(ShopCatalog.Visible(_context).Where(x => x.Slug == slug),
            _context, cancellationToken);

        if (products.Count == 0)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        await SendAsync(ShopCatalog.ToResponse(products[0]), cancellation: cancellationToken);
    }
}

/// <summary>
/// Picture of a product. Its id changes when the picture changes,
/// so browsers can keep it for good.
/// </summary>
public class GetShopImageEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;

    public GetShopImageEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/shop/images/{id}");
        AllowAnonymous();
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);

        var image = await _context.ShopProductImages
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.ContentType, x.Data })
            .FirstOrDefaultAsync(cancellationToken);

        if (image is null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        HttpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        await SendBytesAsync(image.Data, contentType: image.ContentType, cancellation: cancellationToken);
    }
}

/// <summary>
/// Delivery prices and the BoxNow map id for the shop.
/// </summary>
public class GetShopSettingsEndpoint : EndpointWithoutRequest<ShopSettingsResponse>
{
    private readonly ShopSettings _settings;

    public GetShopSettingsEndpoint(ShopSettings settings)
    {
        _settings = settings;
    }

    public override void Configure()
    {
        Get("api/shop/settings");
        AllowAnonymous();
        Options(x => x.WithTags("Shop"));
    }

    public override Task HandleAsync(CancellationToken cancellationToken)
        => SendAsync(new ShopSettingsResponse(_settings.ShippingPrice, _settings.FreeShippingFrom,
            _settings.BoxNowPartnerId, _settings.ClientDiscountPercent), cancellation: cancellationToken);
}
