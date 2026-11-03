using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Hangfire;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public record SetShopOrderStatusRequest(string Status);

/// <summary>
/// Orders of the shop, newest first. Pending ones are checkouts that were
/// not paid (yet), the administration hides them by default.
/// </summary>
public class GetShopAdminOrdersEndpoint : EndpointWithoutRequest<List<ShopOrderResponse>>
{
    private const int MaxOrders = 500;

    private readonly ApexPerformanceContext _context;

    public GetShopAdminOrdersEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/shop/admin/orders");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var orders = await _context.ShopOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxOrders)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        await SendAsync(orders.Select(ShopOrders.ToResponse).ToList(), cancellation: cancellationToken);
    }
}

/// <summary>
/// Marks a paid order shipped, back to paid, or cancelled. Cancelling a paid
/// order returns its pieces to the stock; the money is refunded in Stripe.
/// </summary>
public class SetShopOrderStatusEndpoint : Endpoint<SetShopOrderStatusRequest, ShopOrderResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetShopOrderStatusEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/shop/admin/orders/{id}/status");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(SetShopOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);

        var order = await _context.ShopOrders
            .Include(x => x.Items).ThenInclude(x => x.Variant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        // Unpaid orders follow Stripe, cancelled and refunded ones stay so.
        if (order.Status is ShopOrderStatuses.Pending or ShopOrderStatuses.Cancelled or ShopOrderStatuses.Refunded)
            ThrowError(ErrorCodes.AlreadyChanged, StatusCodes.Status409Conflict);

        if (request.Status == ShopOrderStatuses.Cancelled && order.Status == ShopOrderStatuses.Paid)
        {
            foreach (var item in order.Items)
            {
                item.Variant.Stock += item.Quantity;
                ShopStock.Record(_context, item.Variant, item.Quantity, ShopStockReasons.Return, note: order.Number,
                    orderId: order.Id, userId: _currentUserService.UserId);
            }
        }

        var isShipping = request.Status == ShopOrderStatuses.Shipped && order.Status != ShopOrderStatuses.Shipped;

        order.Status = request.Status;
        order.ShippedAt = request.Status == ShopOrderStatuses.Shipped ? order.ShippedAt ?? DateTimeOffset.UtcNow : null;

        await _context.SaveChangesAsync(cancellationToken);

        // The customer hears once that the parcel is on its way.
        if (isShipping && order.ShippedAt is { } shippedAt && shippedAt > DateTimeOffset.UtcNow.AddMinutes(-1))
            BackgroundJob.Enqueue<ShopOrderService>(service => service.SendShippedEmail(order.Id));

        await SendAsync(ShopOrders.ToResponse(order), cancellation: cancellationToken);
    }
}

public sealed class SetShopOrderStatusValidator : Validator<SetShopOrderStatusRequest>
{
    public SetShopOrderStatusValidator()
    {
        RuleFor(x => x.Status)
            .Must(x => x is ShopOrderStatuses.Paid or ShopOrderStatuses.Shipped or ShopOrderStatuses.Cancelled)
            .WithMessage(ErrorCodes.NotValid);
    }
}
