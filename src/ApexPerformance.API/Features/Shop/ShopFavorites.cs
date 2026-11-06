using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public record MergeShopFavoritesRequest(
    // Favorites kept in the browser before logging in, newest first.
    List<Guid> ProductIds);

internal static class ShopFavorites
{
    // The same limit as in the browser.
    public const int Max = 100;

    // Product ids of the user's favorites, newest first.
    public static Task<List<Guid>> Of(ApexPerformanceContext context, Guid userId, CancellationToken cancellationToken)
        => context.ShopFavorites
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken);

    public static async Task Add(ApexPerformanceContext context, Guid userId, IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().ToList();
        var existing = await context.ShopFavorites
            .Where(x => x.UserId == userId)
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken);
        var known = await context.ShopProducts
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        // Oldest first, so the first id of the list ends up the newest favorite.
        var now = DateTimeOffset.UtcNow;
        var added = ids.Where(x => known.Contains(x) && !existing.Contains(x))
            .Take(Math.Max(0, Max - existing.Count))
            .Reverse()
            .Select((id, index) => new ShopFavorite { UserId = userId, ProductId = id, CreatedAt = now.AddMilliseconds(index) })
            .ToList();

        if (added.Count == 0)
            return;

        context.ShopFavorites.AddRange(added);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The same product was added at the same time from another device.
        }
    }
}

/// <summary>
/// Favorites of the logged in member of the shop, newest first.
/// </summary>
public class GetShopFavoritesEndpoint : EndpointWithoutRequest<List<Guid>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetShopFavoritesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/shop/favorites");
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
        => await SendAsync(await ShopFavorites.Of(_context, _currentUserService.UserId, cancellationToken),
            cancellation: cancellationToken);
}

/// <summary>
/// Adds a product to the favorites of the logged in member. Adding it again changes nothing.
/// </summary>
public class AddShopFavoriteEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddShopFavoriteEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/shop/favorites/{productId}");
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var productId = Route<Guid>("productId", isRequired: true);

        if (!await _context.ShopProducts.AnyAsync(x => x.Id == productId && !x.IsDeleted, cancellationToken))
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        await ShopFavorites.Add(_context, _currentUserService.UserId, [productId], cancellationToken);
        await SendNoContentAsync(cancellationToken);
    }
}

public class RemoveShopFavoriteEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveShopFavoriteEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Delete("api/shop/favorites/{productId}");
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var productId = Route<Guid>("productId", isRequired: true);
        var userId = _currentUserService.UserId;

        await _context.ShopFavorites
            .Where(x => x.UserId == userId && x.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
        await SendNoContentAsync(cancellationToken);
    }
}

/// <summary>
/// After logging in: favorites from the browser join the member's saved ones.
/// Returns all of them, newest first.
/// </summary>
public class MergeShopFavoritesEndpoint : Endpoint<MergeShopFavoritesRequest, List<Guid>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public MergeShopFavoritesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Post("api/shop/favorites/merge");
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(MergeShopFavoritesRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        await ShopFavorites.Add(_context, userId, (request.ProductIds ?? []).Take(ShopFavorites.Max), cancellationToken);
        await SendAsync(await ShopFavorites.Of(_context, userId, cancellationToken), cancellation: cancellationToken);
    }
}
