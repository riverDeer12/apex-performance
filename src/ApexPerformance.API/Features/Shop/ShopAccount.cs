using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Extensions;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public sealed record ShopLoginRequest(string Username, string Password);

public sealed record ShopLoginResponse(string Token);

public sealed record ShopMeResponse(
    string Name,
    string Email,
    // Clients of the studio get the client discount on every product.
    bool IsClient,
    decimal ClientDiscountPercent);

internal static class ShopAccounts
{
    // Client of the studio (not deleted) the logged user is, null for anyone else.
    public static Task<Client?> CurrentClient(ApexPerformanceContext context, Guid userId,
        CancellationToken cancellationToken)
        => userId == Guid.Empty
            ? Task.FromResult<Client?>(null)
            : context.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
}

/// <summary>
/// Login to the webshop with the account of the Apex Performance app (username
/// or email). The shop has its own session: logging in here doesn't log the
/// user out of the app, and logging in to the app doesn't end this one.
/// </summary>
public sealed class ShopLoginEndpoint : Endpoint<ShopLoginRequest, ShopLoginResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IAuthenticationService _authenticationService;

    public ShopLoginEndpoint(ApexPerformanceContext context, IAuthenticationService authenticationService)
    {
        _context = context;
        _authenticationService = authenticationService;
    }

    public override void Configure()
    {
        Post("api/shop/login");
        AllowAnonymous();
        Options(x => x.WithTags("Shop").RequireRateLimiting(RateLimiting.Login));
    }

    public override async Task HandleAsync(ShopLoginRequest request, CancellationToken cancellationToken)
    {
        var login = request.Username.Trim();

        var user = await _context.Users
            .Include(x => x.Roles).ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(x => x.UserName == login || x.Email == login, cancellationToken);

        // The same answer for an unknown user and a wrong password.
        if (user is null || !user.IsValidPassword(request.Password))
            ThrowError(ErrorCodes.WrongUserNameOrPassword);

        var now = DateTimeOffset.UtcNow;
        var expiresAt = _authenticationService.GetTokenExpiration(rememberMe: true);

        // One shop session per user, the app's sessions are not touched.
        await _context.UserSessions
            .Where(x => x.UserId == user.Id && x.Channel == UserSessionChannels.Shop && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x
                    .SetProperty(session => session.RevokedAt, now)
                    .SetProperty(session => session.RevokeReason, SessionRevokeReasons.NewLogin)
                    .SetProperty(session => session.UpdatedAt, now),
                cancellationToken);

        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var session = new UserSession
        {
            UserId = user.Id,
            Channel = UserSessionChannels.Shop,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent.Length > 512 ? userAgent[..512] : userAgent,
            RememberMe = true,
            ExpiresAt = expiresAt,
            CreatedBy = user.Id,
            UpdatedBy = user.Id
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        var token = await _authenticationService.GenerateJwtToken(user, session.Id, expiresAt);

        await SendAsync(new ShopLoginResponse(token), cancellation: cancellationToken);
    }
}

public sealed class ShopLoginValidator : Validator<ShopLoginRequest>
{
    public ShopLoginValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(200).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Password).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(200).WithMessage(ErrorCodes.NotValid);
    }
}

/// <summary>
/// The user logged in to the shop and whether they get the client discount.
/// </summary>
public sealed class GetShopMeEndpoint : EndpointWithoutRequest<ShopMeResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ShopSettings _settings;

    public GetShopMeEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        ShopSettings settings)
    {
        _context = context;
        _currentUserService = currentUserService;
        _settings = settings;
    }

    public override void Configure()
    {
        Get("api/shop/me");
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
            ThrowError(ErrorCodes.UserNotFound, StatusCodes.Status404NotFound);

        var client = await ShopAccounts.CurrentClient(_context, user.Id, cancellationToken);

        await SendAsync(new ShopMeResponse(client?.FullName ?? user.UserName, client?.Email ?? user.Email,
                client is not null, client is null ? 0 : _settings.ClientDiscountPercent),
            cancellation: cancellationToken);
    }
}

/// <summary>
/// Orders the logged user made while logged in to the shop, newest first.
/// Unpaid checkouts are left out.
/// </summary>
public sealed class GetMyShopOrdersEndpoint : EndpointWithoutRequest<List<ShopOrderResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyShopOrdersEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/shop/my-orders");
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var orders = await _context.ShopOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerUserId == _currentUserService.UserId && x.Status != ShopOrderStatuses.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        await SendAsync(orders.Select(ShopOrders.ToResponse).ToList(), cancellation: cancellationToken);
    }
}
