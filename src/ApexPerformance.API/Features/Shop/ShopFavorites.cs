using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

// Favorite product with the size and color chosen when it was marked, if any.
public record ShopFavoriteItem(Guid ProductId, string? Size = null, string? Color = null);

public record MergeShopFavoritesRequest(
    // Favorites kept in the browser before logging in, newest first.
    List<ShopFavoriteItem> Items);

public record SaveShopFavoriteRequest(string? Size, string? Color);

internal static class ShopFavorites
{
    // The same limit as in the browser.
    public const int Max = 100;

    // The user's favorites, newest first.
    public static Task<List<ShopFavoriteItem>> Of(ApexPerformanceContext context, Guid userId,
        CancellationToken cancellationToken)
        => context.ShopFavorites
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ShopFavoriteItem(x.ProductId, x.Size, x.Color))
            .ToListAsync(cancellationToken);

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 50)];

    /// <summary>
    /// Adds favorites the user doesn't have yet; one already there gets the new size and color.
    /// </summary>
    public static async Task Add(ApexPerformanceContext context, Guid userId, IEnumerable<ShopFavoriteItem> items,
        CancellationToken cancellationToken)
    {
        var list = items.GroupBy(x => x.ProductId).Select(x => x.First()).ToList();
        var ids = list.Select(x => x.ProductId).ToList();
        var saved = await context.ShopFavorites
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
        var existing = saved.Select(x => x.ProductId).ToList();

        foreach (var favorite in saved)
            if (list.FirstOrDefault(x => x.ProductId == favorite.ProductId) is { } item &&
                (item.Size is not null || item.Color is not null))
            {
                favorite.Size = Clean(item.Size);
                favorite.Color = Clean(item.Color);
            }
        var known = await context.ShopProducts
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        // Oldest first, so the first id of the list ends up the newest favorite.
        var now = DateTimeOffset.UtcNow;
        var added = ids.Where(x => known.Contains(x) && !existing.Contains(x))
            .Take(Math.Max(0, Max - existing.Count))
            .Reverse()
            .Select((id, index) =>
            {
                var item = list.First(x => x.ProductId == id);
                return new ShopFavorite
                {
                    UserId = userId, ProductId = id, Size = Clean(item.Size), Color = Clean(item.Color),
                    CreatedAt = now.AddMilliseconds(index)
                };
            })
            .ToList();

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
public class GetShopFavoritesEndpoint : EndpointWithoutRequest<List<ShopFavoriteItem>>
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
/// Adds a product to the favorites of the logged in member, with the chosen size and color.
/// Adding it again keeps it and updates the size and color.
/// </summary>
public class AddShopFavoriteEndpoint : Endpoint<SaveShopFavoriteRequest>
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

    public override async Task HandleAsync(SaveShopFavoriteRequest request, CancellationToken cancellationToken)
    {
        var productId = Route<Guid>("productId", isRequired: true);

        if (!await _context.ShopProducts.AnyAsync(x => x.Id == productId && !x.IsDeleted, cancellationToken))
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        await ShopFavorites.Add(_context, _currentUserService.UserId,
            [new ShopFavoriteItem(productId, request.Size, request.Color)], cancellationToken);
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
public class MergeShopFavoritesEndpoint : Endpoint<MergeShopFavoritesRequest, List<ShopFavoriteItem>>
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
        await ShopFavorites.Add(_context, userId, (request.Items ?? []).Take(ShopFavorites.Max), cancellationToken);
        await SendAsync(await ShopFavorites.Of(_context, userId, cancellationToken), cancellation: cancellationToken);
    }
}
