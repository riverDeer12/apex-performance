using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public record ShopStockRowResponse(
    Guid VariantId,
    Guid ProductId,
    string ProductName,
    string Category,
    string? Size,
    string? Color,
    int Stock,
    decimal Price,
    bool ProductIsActive,
    Guid? ImageId,
    // Pieces sold in the last 30 days, to see what runs out.
    int SoldLast30Days,
    DateTimeOffset? LastMovementAt);

public record AdjustShopStockRequest(
    // Receipt (+quantity), WriteOff (-quantity) or Correction (stock counted = quantity).
    string Reason,
    int Quantity,
    string? Note);

public record ShopStockMovementResponse(
    Guid Id,
    int Change,
    int StockAfter,
    string Reason,
    string? Note,
    string? OrderNumber,
    string? CreatedByName,
    DateTimeOffset CreatedAt);

/// <summary>
/// Warehouse: every size and color of the products still sold, with its stock.
/// </summary>
public class GetShopStockEndpoint : EndpointWithoutRequest<List<ShopStockRowResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetShopStockEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/shop/admin/stock");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-30);

        var rows = await _context.ShopProductVariants
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.Product.IsDeleted)
            .Select(x => new ShopStockRowResponse(
                x.Id,
                x.ProductId,
                x.Product.Name,
                x.Product.Category,
                x.Size,
                x.Color,
                x.Stock,
                x.Product.Price,
                x.Product.IsActive,
                x.Product.Images.OrderBy(i => i.SortOrder).Select(i => (Guid?)i.Id).FirstOrDefault(),
                -_context.ShopStockMovements
                    .Where(m => m.VariantId == x.Id && m.Reason == ShopStockReasons.Sale && m.CreatedAt >= since)
                    .Sum(m => (int?)m.Change) ?? 0,
                _context.ShopStockMovements
                    .Where(m => m.VariantId == x.Id)
                    .Max(m => (DateTimeOffset?)m.CreatedAt)))
            .ToListAsync(cancellationToken);

        await SendAsync(rows
                .OrderBy(x => x.ProductName)
                .ThenBy(x => x.Size)
                .ThenBy(x => x.Color)
                .ToList(),
            cancellation: cancellationToken);
    }
}

/// <summary>
/// Goods received, written off or counted. Returns the new row of the warehouse.
/// </summary>
public class AdjustShopStockEndpoint : Endpoint<AdjustShopStockRequest, ShopStockRowResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AdjustShopStockEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Post("api/shop/admin/stock/{variantId}");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(AdjustShopStockRequest request, CancellationToken cancellationToken)
    {
        var variantId = Route<Guid>("variantId", isRequired: true);

        var variant = await _context.ShopProductVariants
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == variantId && !x.IsDeleted, cancellationToken);

        if (variant is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var newStock = request.Reason switch
        {
            ShopStockReasons.Receipt => variant.Stock + request.Quantity,
            ShopStockReasons.WriteOff => variant.Stock - request.Quantity,
            _ => request.Quantity
        };

        if (newStock < 0)
            ThrowError(ErrorCodes.NotValid);

        var change = newStock - variant.Stock;
        variant.Stock = newStock;

        ShopStock.Record(_context, variant, change, request.Reason, request.Note, userId: _currentUserService.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        var imageId = await _context.ShopProductImages
            .Where(x => x.ProductId == variant.ProductId)
            .OrderBy(x => x.SortOrder)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var sold = -await _context.ShopStockMovements
            .Where(m => m.VariantId == variant.Id && m.Reason == ShopStockReasons.Sale && m.CreatedAt >= since)
            .SumAsync(m => (int?)m.Change, cancellationToken) ?? 0;

        await SendAsync(new ShopStockRowResponse(variant.Id, variant.ProductId, variant.Product.Name,
                variant.Product.Category, variant.Size, variant.Color, variant.Stock, variant.Product.Price,
                variant.Product.IsActive, imageId, sold, DateTimeOffset.UtcNow),
            cancellation: cancellationToken);
    }
}

public sealed class AdjustShopStockValidator : Validator<AdjustShopStockRequest>
{
    public AdjustShopStockValidator()
    {
        RuleFor(x => x.Reason).Must(x => ShopStockReasons.Manual.Contains(x)).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Reason != ShopStockReasons.Correction)
            .WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Quantity).InclusiveBetween(0, 100000).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Note).MaximumLength(300).WithMessage(ErrorCodes.NotValid);
    }
}

/// <summary>
/// Last 200 changes of the stock of a variant, newest first.
/// </summary>
public class GetShopStockMovementsEndpoint : EndpointWithoutRequest<List<ShopStockMovementResponse>>
{
    private const int MaxMovements = 200;

    private readonly ApexPerformanceContext _context;

    public GetShopStockMovementsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/shop/admin/stock/{variantId}/movements");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var variantId = Route<Guid>("variantId", isRequired: true);

        var movements = await _context.ShopStockMovements
            .AsNoTracking()
            .Where(x => x.VariantId == variantId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxMovements)
            .Select(x => new ShopStockMovementResponse(
                x.Id,
                x.Change,
                x.StockAfter,
                x.Reason,
                x.Note,
                _context.ShopOrders.Where(o => o.Id == x.OrderId).Select(o => o.Number).FirstOrDefault(),
                _context.Users.Where(u => u.Id == x.CreatedBy).Select(u => u.UserName).FirstOrDefault(),
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        await SendAsync(movements, cancellation: cancellationToken);
    }
}
