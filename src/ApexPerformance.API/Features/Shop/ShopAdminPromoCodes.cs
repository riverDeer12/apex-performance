using System.Text.RegularExpressions;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public record ShopPromoCodeResponse(
    Guid Id,
    string Code,
    string DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    int? MaxUses,
    bool IsActive,
    string? Note,
    // Empty when the code is for all products.
    List<Guid> ProductIds,
    // Paid orders that used it.
    int Uses,
    decimal TotalDiscount);

public record SaveShopPromoCodeRequest(
    string Code,
    string DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    int? MaxUses,
    bool IsActive,
    string? Note,
    List<Guid> ProductIds);

internal static class ShopPromoCodes
{
    public static async Task<List<ShopPromoCodeResponse>> Load(ApexPerformanceContext context, Guid? id,
        CancellationToken cancellationToken)
    {
        var promos = await context.ShopPromoCodes
            .AsNoTracking()
            .Include(x => x.Products)
            .Where(x => !x.IsDeleted && (id == null || x.Id == id))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var codes = promos.Select(x => x.Code).ToList();

        var uses = await context.ShopOrders
            .AsNoTracking()
            .Where(x => x.PromoCode != null && codes.Contains(x.PromoCode) &&
                        (x.Status == ShopOrderStatuses.Paid || x.Status == ShopOrderStatuses.Shipped))
            .GroupBy(x => x.PromoCode!)
            .Select(x => new { Code = x.Key, Count = x.Count(), Discount = x.Sum(o => o.Discount) })
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        return promos.Select(x => new ShopPromoCodeResponse(x.Id, x.Code, x.DiscountType, x.DiscountValue, x.ValidFrom,
                x.ValidUntil, x.MaxUses, x.IsActive, x.Note, x.Products.Select(p => p.ProductId).ToList(),
                uses.GetValueOrDefault(x.Code)?.Count ?? 0, uses.GetValueOrDefault(x.Code)?.Discount ?? 0))
            .ToList();
    }

    public static async Task<string?> Apply(ApexPerformanceContext context, ShopPromoCode promo,
        SaveShopPromoCodeRequest request, CancellationToken cancellationToken)
    {
        var code = ShopPromo.Normalize(request.Code);

        // A deleted code can be used again, past orders keep the code as text.
        if (await context.ShopPromoCodes.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != promo.Id,
                cancellationToken))
            return ErrorCodes.AlreadyExists;

        var productIds = request.ProductIds.Distinct().ToList();

        if (await context.ShopProducts.CountAsync(x => productIds.Contains(x.Id), cancellationToken) != productIds.Count)
            return ErrorCodes.NotFound;

        promo.Code = code;
        promo.DiscountType = request.DiscountType;
        promo.DiscountValue = request.DiscountValue;
        promo.ValidFrom = request.ValidFrom;
        promo.ValidUntil = request.ValidUntil;
        promo.MaxUses = request.MaxUses;
        promo.IsActive = request.IsActive;
        promo.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        promo.Products.Clear();
        foreach (var productId in productIds)
            promo.Products.Add(new ShopPromoCodeProduct { ProductId = productId });

        return null;
    }
}

public sealed partial class SaveShopPromoCodeValidator : Validator<SaveShopPromoCodeRequest>
{
    public SaveShopPromoCodeValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(ErrorCodes.Required)
            .Must(x => CodePattern().IsMatch(x?.Trim() ?? "")).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.DiscountType)
            .Must(x => x is ShopDiscountTypes.Percent or ShopDiscountTypes.Amount).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.DiscountValue).GreaterThan(0).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100).When(x => x.DiscountType == ShopDiscountTypes.Percent)
            .WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.DiscountValue).LessThan(10000).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.MaxUses).GreaterThan(0).When(x => x.MaxUses is not null).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.ValidUntil).GreaterThan(x => x.ValidFrom).When(x => x.ValidFrom is not null && x.ValidUntil is not null)
            .WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Note).MaximumLength(500).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.ProductIds).NotNull().WithMessage(ErrorCodes.Required);
    }

    // Letters, digits, "-" and "_", 3 to 40 characters, e.g. "APEX10".
    [GeneratedRegex("^[A-Za-z0-9_-]{3,40}$")]
    private static partial Regex CodePattern();
}

public class GetShopPromoCodesEndpoint : EndpointWithoutRequest<List<ShopPromoCodeResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetShopPromoCodesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/shop/admin/promo-codes");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
        => await SendAsync(await ShopPromoCodes.Load(_context, null, cancellationToken), cancellation: cancellationToken);
}

public class CreateShopPromoCodeEndpoint : Endpoint<SaveShopPromoCodeRequest, ShopPromoCodeResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateShopPromoCodeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/shop/admin/promo-codes");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(SaveShopPromoCodeRequest request, CancellationToken cancellationToken)
    {
        var promo = new ShopPromoCode { Code = "", DiscountType = "" };

        var error = await ShopPromoCodes.Apply(_context, promo, request, cancellationToken);

        if (error is not null)
            ThrowError(error);

        _context.ShopPromoCodes.Add(promo);
        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync((await ShopPromoCodes.Load(_context, promo.Id, cancellationToken))[0],
            cancellation: cancellationToken);
    }
}

public class UpdateShopPromoCodeEndpoint : Endpoint<SaveShopPromoCodeRequest, ShopPromoCodeResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateShopPromoCodeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/shop/admin/promo-codes/{id}");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(SaveShopPromoCodeRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);

        var promo = await _context.ShopPromoCodes
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (promo is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        // Orders keep the code as text, renaming a used code would cut them off from it.
        if (ShopPromo.Normalize(request.Code) != promo.Code &&
            await ShopPromo.Uses(_context, promo.Code).AnyAsync(cancellationToken))
            ThrowError(ErrorCodes.AlreadyChanged);

        var error = await ShopPromoCodes.Apply(_context, promo, request, cancellationToken);

        if (error is not null)
            ThrowError(error);

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync((await ShopPromoCodes.Load(_context, promo.Id, cancellationToken))[0],
            cancellation: cancellationToken);
    }
}

public class DeleteShopPromoCodeEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;

    public DeleteShopPromoCodeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/shop/admin/promo-codes/{id}");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);

        var promo = await _context.ShopPromoCodes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (promo is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        promo.Delete();
        promo.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);

        await SendNoContentAsync(cancellationToken);
    }
}
