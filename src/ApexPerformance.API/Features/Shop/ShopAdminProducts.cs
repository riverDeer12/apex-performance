using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Profile;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public record ShopAdminVariantDto(
    // Empty for a new variant.
    Guid? Id,
    string? Size,
    string? Color,
    int Stock);

public record ShopAdminProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Category,
    decimal Price,
    bool IsActive,
    int SortOrder,
    List<Guid> ImageIds,
    List<ShopAdminVariantDto> Variants);

public record SaveShopProductRequest(
    string Name,
    // Made from the name when empty.
    string? Slug,
    string Description,
    string Category,
    decimal Price,
    bool IsActive,
    int SortOrder,
    // Full list in the shown order: variants left out are removed.
    List<ShopAdminVariantDto> Variants);

public record ReorderShopImagesRequest(List<Guid> ImageIds);

internal static class ShopAdmin
{
    public static readonly string[] Roles = [UserRoles.SuperAdmin, UserRoles.Administrator];

    public static ShopAdminProductResponse ToResponse(ShopProduct product)
        => new(product.Id, product.Name, product.Slug, product.Description, product.Category, product.Price,
            product.IsActive, product.SortOrder,
            product.Images.OrderBy(x => x.SortOrder).Select(x => x.Id).ToList(),
            product.Variants
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .Select(x => new ShopAdminVariantDto(x.Id, x.Size, x.Color, x.Stock))
                .ToList());

    public static async Task<ShopAdminProductResponse> Load(ApexPerformanceContext context, Guid id,
        CancellationToken cancellationToken)
    {
        var products = await ShopProductLoader.Load(
            context.ShopProducts.AsNoTracking().Where(x => x.Id == id), context, cancellationToken);

        return ToResponse(products[0]);
    }

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Saves the fields and the variants of a product. Variants missing from the
    /// request are only marked removed, past orders keep pointing to them.
    /// </summary>
    public static async Task<string?> Apply(ApexPerformanceContext context, ShopProduct product,
        SaveShopProductRequest request, CancellationToken cancellationToken)
    {
        var slug = ShopSettings.ToSlug(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);

        if (slug.Length == 0)
            return ErrorCodes.NotValid;

        if (await context.ShopProducts.AnyAsync(x => x.Slug == slug && x.Id != product.Id, cancellationToken))
            return ErrorCodes.AlreadyExists;

        var keys = request.Variants.Select(x => (Clean(x.Size)?.ToLowerInvariant(), Clean(x.Color)?.ToLowerInvariant())).ToList();

        if (keys.Distinct().Count() != keys.Count)
            return ErrorCodes.DuplicatesNotAllowed;

        product.Name = request.Name.Trim();
        product.Slug = slug;
        product.Description = request.Description.Trim();
        product.Category = request.Category.Trim();
        product.Price = request.Price;
        product.IsActive = request.IsActive;
        product.SortOrder = request.SortOrder;

        var kept = new HashSet<Guid>();

        for (var i = 0; i < request.Variants.Count; i++)
        {
            var dto = request.Variants[i];
            var variant = dto.Id is { } id ? product.Variants.FirstOrDefault(x => x.Id == id && !x.IsDeleted) : null;

            if (variant is null)
            {
                variant = new ShopProductVariant();
                product.Variants.Add(variant);
            }
            else
            {
                kept.Add(variant.Id);
            }

            variant.Size = Clean(dto.Size);
            variant.Color = Clean(dto.Color);
            variant.Stock = dto.Stock;
            variant.SortOrder = i;
        }

        foreach (var removed in product.Variants.Where(x => x.Id != Guid.Empty && !kept.Contains(x.Id)))
            removed.IsDeleted = true;

        return null;
    }
}

public sealed class SaveShopProductValidator : Validator<SaveShopProductRequest>
{
    public SaveShopProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(200).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Slug).MaximumLength(200).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(4000).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Category).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(100).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage(ErrorCodes.NotValid).LessThan(100000).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Variants).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleForEach(x => x.Variants).ChildRules(variant =>
        {
            variant.RuleFor(x => x.Size).MaximumLength(50).WithMessage(ErrorCodes.NotValid);
            variant.RuleFor(x => x.Color).MaximumLength(50).WithMessage(ErrorCodes.NotValid);
            variant.RuleFor(x => x.Stock).InclusiveBetween(0, 100000).WithMessage(ErrorCodes.NotValid);
        });
    }
}

/// <summary>
/// All products of the shop, inactive ones too, for the administration.
/// </summary>
public class GetShopAdminProductsEndpoint : EndpointWithoutRequest<List<ShopAdminProductResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetShopAdminProductsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/shop/admin/products");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var products = await ShopProductLoader.Load(
            _context.ShopProducts.AsNoTracking().Where(x => !x.IsDeleted), _context, cancellationToken);

        await SendAsync(products
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(ShopAdmin.ToResponse)
                .ToList(),
            cancellation: cancellationToken);
    }
}

public class CreateShopProductEndpoint : Endpoint<SaveShopProductRequest, ShopAdminProductResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateShopProductEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/shop/admin/products");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(SaveShopProductRequest request, CancellationToken cancellationToken)
    {
        var product = new ShopProduct { Name = "", Slug = "", Description = "", Category = "" };

        var error = await ShopAdmin.Apply(_context, product, request, cancellationToken);

        if (error is not null)
            ThrowError(error);

        _context.ShopProducts.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(await ShopAdmin.Load(_context, product.Id, cancellationToken), cancellation: cancellationToken);
    }
}

public class UpdateShopProductEndpoint : Endpoint<SaveShopProductRequest, ShopAdminProductResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateShopProductEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/shop/admin/products/{id}");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(SaveShopProductRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);

        var product = await _context.ShopProducts
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (product is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var error = await ShopAdmin.Apply(_context, product, request, cancellationToken);

        if (error is not null)
            ThrowError(error);

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(await ShopAdmin.Load(_context, product.Id, cancellationToken), cancellation: cancellationToken);
    }
}

/// <summary>
/// Removes a product from the shop and the administration. Past orders keep it.
/// </summary>
public class DeleteShopProductEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;

    public DeleteShopProductEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/shop/admin/products/{id}");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);

        var product = await _context.ShopProducts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (product is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        product.Delete();
        product.IsActive = false;
        // The slug can be used again by a new product.
        product.Slug = $"deleted-{product.Id:N}";

        await _context.SaveChangesAsync(cancellationToken);

        await SendNoContentAsync(cancellationToken);
    }
}

/// <summary>
/// Adds pictures (JPEG, PNG or WebP, at most 5 MB each) after the existing ones.
/// </summary>
public class AddShopImagesEndpoint : EndpointWithoutRequest<ShopAdminProductResponse>
{
    private const int MaxSizeInBytes = 5 * 1024 * 1024;
    private const int MaxImages = 12;

    private readonly ApexPerformanceContext _context;

    public AddShopImagesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/shop/admin/products/{id}/images");
        Roles(ShopAdmin.Roles);
        AllowFileUploads();
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);

        if (!await _context.ShopProducts.AnyAsync(x => x.Id == id && !x.IsDeleted, cancellationToken))
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        // Any number of "files" parts in the form.
        var form = await HttpContext.Request.ReadFormAsync(cancellationToken);
        var files = form.Files.Where(x => x.Length > 0).ToList();

        if (files.Count == 0)
            ThrowError(ErrorCodes.Required);

        var existing = await _context.ShopProductImages
            .Where(x => x.ProductId == id)
            .Select(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        if (existing.Count + files.Count > MaxImages)
            ThrowError(ErrorCodes.NotValid);

        var sortOrder = existing.Count == 0 ? 0 : existing.Max() + 1;

        foreach (var file in files)
        {
            if (file.Length > MaxSizeInBytes)
                ThrowError(ErrorCodes.NotValid, StatusCodes.Status413PayloadTooLarge);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var data = memoryStream.ToArray();

            // From the file's content, not its name or the header the browser sent.
            var contentType = ProfilePictureRules.DetectContentType(data);

            if (contentType is null)
                ThrowError(ErrorCodes.NotValid);

            _context.ShopProductImages.Add(new ShopProductImage
            {
                ProductId = id,
                ContentType = contentType,
                Data = data,
                SortOrder = sortOrder++,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(await ShopAdmin.Load(_context, id, cancellationToken), cancellation: cancellationToken);
    }
}

/// <summary>
/// Sets the order of the pictures, the first one is the main picture.
/// </summary>
public class ReorderShopImagesEndpoint : Endpoint<ReorderShopImagesRequest, ShopAdminProductResponse>
{
    private readonly ApexPerformanceContext _context;

    public ReorderShopImagesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/shop/admin/products/{id}/images");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(ReorderShopImagesRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);

        var images = await _context.ShopProductImages
            .Where(x => x.ProductId == id)
            .Select(x => new { x.Id, x.SortOrder })
            .ToListAsync(cancellationToken);

        if (images.Count == 0 || images.Count != request.ImageIds.Count ||
            images.Any(x => !request.ImageIds.Contains(x.Id)))
            ThrowError(ErrorCodes.NotValid);

        for (var i = 0; i < request.ImageIds.Count; i++)
        {
            var imageId = request.ImageIds[i];
            var order = i;

            await _context.ShopProductImages
                .Where(x => x.Id == imageId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.SortOrder, order), cancellationToken);
        }

        await SendAsync(await ShopAdmin.Load(_context, id, cancellationToken), cancellation: cancellationToken);
    }
}

public class DeleteShopImageEndpoint : EndpointWithoutRequest<ShopAdminProductResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteShopImageEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/shop/admin/products/{id}/images/{imageId}");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);
        var imageId = Route<Guid>("imageId", isRequired: true);

        var deleted = await _context.ShopProductImages
            .Where(x => x.Id == imageId && x.ProductId == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        await SendAsync(await ShopAdmin.Load(_context, id, cancellationToken), cancellation: cancellationToken);
    }
}
