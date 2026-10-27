using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Extensions;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace ApexPerformance.API.Features.Shop;

public record ShopCartItemDto(Guid VariantId, int Quantity);

public record ShopCheckoutRequest(
    List<ShopCartItemDto> Items,
    // Locker chosen on the BoxNow map in the shop.
    string BoxNowLockerId,
    string BoxNowLockerAddress,
    string BoxNowLockerPostalCode);

public record ShopCheckoutResponse(Guid OrderId, string CheckoutUrl);

public record ShopOrderItemResponse(string ProductName, string? VariantName, decimal UnitPrice, int Quantity);

public record ShopOrderResponse(
    Guid Id,
    string Number,
    string Status,
    string? CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    decimal Subtotal,
    decimal ShippingPrice,
    decimal Total,
    string BoxNowLockerId,
    string BoxNowLockerAddress,
    string BoxNowLockerPostalCode,
    string? BoxNowParcelId,
    bool HasStockIssue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    List<ShopOrderItemResponse> Items);

internal static class ShopOrders
{
    public static ShopOrderResponse ToResponse(ShopOrder order)
        => new(order.Id, order.Number, order.Status, order.CustomerName, order.CustomerEmail, order.CustomerPhone,
            order.Subtotal, order.ShippingPrice, order.Total, order.BoxNowLockerId, order.BoxNowLockerAddress,
            order.BoxNowLockerPostalCode, order.BoxNowParcelId, order.HasStockIssue, order.CreatedAt, order.PaidAt,
            order.ShippedAt,
            order.Items.Select(x => new ShopOrderItemResponse(x.ProductName, x.VariantName, x.UnitPrice, x.Quantity))
                .ToList());

    // "AP-261027-4821": date and a random part, short enough to read on the phone.
    public static async Task<string> NewNumber(ApexPerformanceContext context, CancellationToken cancellationToken)
    {
        while (true)
        {
            var number = $"AP-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 10000)}";

            if (!await context.ShopOrders.AnyAsync(x => x.Number == number, cancellationToken))
                return number;
        }
    }
}

/// <summary>
/// Starts the payment of the cart: checks the prices and stock in the database
/// (never the ones the browser sends), saves a pending order and opens Stripe
/// Checkout for it. The shop sends the customer to the returned address.
/// </summary>
public class ShopCheckoutEndpoint : Endpoint<ShopCheckoutRequest, ShopCheckoutResponse>
{
    // Unpaid checkouts expire, the order is then cancelled by ShopOrderSyncJob.
    private static readonly TimeSpan CheckoutValidFor = TimeSpan.FromHours(1);

    private readonly ApexPerformanceContext _context;
    private readonly ShopSettings _settings;
    private readonly ILogger<ShopCheckoutEndpoint> _logger;

    public ShopCheckoutEndpoint(ApexPerformanceContext context, ShopSettings settings,
        ILogger<ShopCheckoutEndpoint> logger)
    {
        _context = context;
        _settings = settings;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("api/shop/checkout");
        AllowAnonymous();
        Options(x => x.WithTags("Shop").RequireRateLimiting(RateLimiting.ShopCheckout));
    }

    public override async Task HandleAsync(ShopCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_settings.StripeSecretKey))
        {
            _logger.LogError("Shop checkout is not possible, Stripe:ApexPerformance:SecretKey is not set.");
            ThrowError(ErrorCodes.SavingError, StatusCodes.Status503ServiceUnavailable);
        }

        var variantIds = request.Items.Select(x => x.VariantId).ToList();

        var variants = await _context.ShopProductVariants
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => variantIds.Contains(x.Id) && !x.IsDeleted && x.Product.IsActive && !x.Product.IsDeleted)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        // A product was removed or hidden while it was in the cart.
        if (variants.Count != variantIds.Count)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        if (request.Items.Any(x => variants[x.VariantId].Stock < x.Quantity))
            ThrowError(ErrorCodes.OutOfStock, StatusCodes.Status409Conflict);

        var order = new ShopOrder
        {
            Number = await ShopOrders.NewNumber(_context, cancellationToken),
            Status = ShopOrderStatuses.Pending,
            BoxNowLockerId = request.BoxNowLockerId.Trim(),
            BoxNowLockerAddress = request.BoxNowLockerAddress.Trim(),
            BoxNowLockerPostalCode = request.BoxNowLockerPostalCode.Trim()
        };

        foreach (var item in request.Items)
        {
            var variant = variants[item.VariantId];
            var variantName = ShopSettings.VariantName(variant);

            order.Items.Add(new ShopOrderItem
            {
                VariantId = variant.Id,
                ProductName = variant.Product.Name,
                VariantName = variantName.Length == 0 ? null : variantName,
                UnitPrice = variant.Product.Price,
                Quantity = item.Quantity
            });
        }

        order.Subtotal = order.Items.Sum(x => x.UnitPrice * x.Quantity);
        order.ShippingPrice = _settings.ShippingFor(order.Subtotal);
        order.Total = order.Subtotal + order.ShippingPrice;

        _context.ShopOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        var metadata = new Dictionary<string, string>
        {
            ["OrderId"] = order.Id.ToString(),
            ["OrderNumber"] = order.Number,
            ["BoxNowLockerId"] = order.BoxNowLockerId,
            ["BoxNowLockerAddressLine1"] = order.BoxNowLockerAddress,
            ["BoxNowLockerPostalCode"] = order.BoxNowLockerPostalCode
        };

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            Locale = "hr",
            ClientReferenceId = order.Id.ToString(),
            LineItems = order.Items.Select(item => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "eur",
                    // The name is on the receipt eRačuni makes from the payment.
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = string.IsNullOrEmpty(item.VariantName)
                            ? item.ProductName
                            : $"{item.ProductName} ({item.VariantName})"
                    },
                    UnitAmount = ToCents(item.UnitPrice)
                },
                Quantity = item.Quantity
            }).ToList(),
            ShippingOptions =
            [
                new SessionShippingOptionOptions
                {
                    ShippingRateData = new SessionShippingOptionShippingRateDataOptions
                    {
                        DisplayName = "BoxNow paketomat",
                        Type = "fixed_amount",
                        FixedAmount = new SessionShippingOptionShippingRateDataFixedAmountOptions
                        {
                            Amount = ToCents(order.ShippingPrice),
                            Currency = "eur"
                        }
                    }
                }
            ],
            BillingAddressCollection = "required",
            CustomerCreation = "always",
            // BoxNow sends the code for the locker to the phone.
            PhoneNumberCollection = new SessionPhoneNumberCollectionOptions { Enabled = true },
            Metadata = metadata,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Description = $"Apex Performance Shop – narudžba {order.Number}",
                Metadata = metadata
            },
            ExpiresAt = DateTime.UtcNow.Add(CheckoutValidFor),
            SuccessUrl = $"{_settings.ShopUrl}/narudzba/{order.Id}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_settings.ShopUrl}/kosarica"
        };

        var session = await new SessionService().CreateAsync(options,
            new RequestOptions { ApiKey = _settings.StripeSecretKey }, cancellationToken);

        order.StripeSessionId = session.Id;
        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(new ShopCheckoutResponse(order.Id, session.Url), cancellation: cancellationToken);
    }

    private static long ToCents(decimal value) => (long)Math.Round(value * 100, MidpointRounding.AwayFromZero);
}

public sealed class ShopCheckoutValidator : Validator<ShopCheckoutRequest>
{
    public ShopCheckoutValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage(ErrorCodes.Required)
            .Must(x => x.Count <= 30).WithMessage(ErrorCodes.NotValid)
            .Must(x => x.Select(i => i.VariantId).Distinct().Count() == x.Count).WithMessage(ErrorCodes.DuplicatesNotAllowed);
        RuleForEach(x => x.Items).ChildRules(item =>
            item.RuleFor(x => x.Quantity).InclusiveBetween(1, 20).WithMessage(ErrorCodes.NotValid));
        RuleFor(x => x.BoxNowLockerId).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(50).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.BoxNowLockerAddress).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(300).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.BoxNowLockerPostalCode).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(20).WithMessage(ErrorCodes.NotValid);
    }
}

/// <summary>
/// The order the customer just paid, for the page they return to from Stripe.
/// The checkout session id from that address is needed, so only the customer
/// sees it. Confirms the payment right away instead of waiting for ShopOrderSyncJob.
/// </summary>
public class GetShopOrderEndpoint : EndpointWithoutRequest<ShopOrderResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ShopOrderService _orderService;

    public GetShopOrderEndpoint(ApexPerformanceContext context, ShopOrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    public override void Configure()
    {
        Get("api/shop/orders/{id}");
        AllowAnonymous();
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id", isRequired: true);
        var sessionId = Query<string>("sessionId", isRequired: false);

        var exists = await _context.ShopOrders.AnyAsync(x =>
            x.Id == id && x.StripeSessionId != null && x.StripeSessionId == sessionId, cancellationToken);

        if (!exists)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        await _orderService.ConfirmAsync(id, cancellationToken);

        var order = await _context.ShopOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == id, cancellationToken);

        await SendAsync(ShopOrders.ToResponse(order), cancellation: cancellationToken);
    }
}
