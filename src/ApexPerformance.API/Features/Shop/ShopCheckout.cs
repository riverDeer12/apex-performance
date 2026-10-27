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
    string BoxNowLockerPostalCode,
    // Optional, checked again here.
    string? PromoCode = null);

public record ShopPromoCheckRequest(string Code, List<ShopCartItemDto> Items);

public record ShopPromoItemResponse(Guid VariantId, decimal UnitDiscount);

public record ShopPromoCheckResponse(
    string Code,
    string DiscountType,
    decimal DiscountValue,
    // Whole discount for the given cart.
    decimal Discount,
    List<ShopPromoItemResponse> Items);

public record ShopCheckoutResponse(Guid OrderId, string CheckoutUrl);

public record ShopOrderItemResponse(string ProductName, string? VariantName, decimal UnitPrice, decimal UnitDiscount,
    int Quantity);

public record ShopOrderResponse(
    Guid Id,
    string Number,
    string Status,
    string? CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    decimal Subtotal,
    string? PromoCode,
    decimal Discount,
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
            order.Subtotal, order.PromoCode, order.Discount, order.ShippingPrice, order.Total, order.BoxNowLockerId, order.BoxNowLockerAddress,
            order.BoxNowLockerPostalCode, order.BoxNowParcelId, order.HasStockIssue, order.CreatedAt, order.PaidAt,
            order.ShippedAt,
            order.Items.Select(x => new ShopOrderItemResponse(x.ProductName, x.VariantName, x.UnitPrice, x.UnitDiscount,
                    x.Quantity))
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

        ShopPromoCode? promo = null;

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            (promo, var promoError) = await ShopPromo.Find(_context, request.PromoCode, cancellationToken);

            if (promoError is not null)
                ThrowError(promoError);
        }

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
                UnitDiscount = promo is null ? 0 : ShopPromo.UnitDiscount(promo, variant.ProductId, variant.Product.Price),
                Quantity = item.Quantity
            });
        }

        if (promo is not null)
        {
            if (order.Items.All(x => x.UnitDiscount == 0))
                ThrowError(ErrorCodes.PromoCodeNotApplicable);

            order.PromoCode = promo.Code;
        }

        order.Subtotal = order.Items.Sum(x => x.UnitPrice * x.Quantity);
        order.Discount = order.Items.Sum(x => x.UnitDiscount * x.Quantity);
        // Free delivery counts what the customer pays for the products.
        order.ShippingPrice = _settings.ShippingFor(order.Subtotal - order.Discount);
        order.Total = order.Subtotal - order.Discount + order.ShippingPrice;

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
                        Name = (string.IsNullOrEmpty(item.VariantName)
                                   ? item.ProductName
                                   : $"{item.ProductName} ({item.VariantName})") +
                               (item.UnitDiscount > 0 ? $" – promo kod {order.PromoCode}" : "")
                    },
                    // The receipt shows the price the customer pays.
                    UnitAmount = ToCents(item.UnitPrice - item.UnitDiscount)
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
        RuleFor(x => x.PromoCode).MaximumLength(40).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.BoxNowLockerPostalCode).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(20).WithMessage(ErrorCodes.NotValid);
    }
}

/// <summary>
/// Checks a promo code for the cart and returns its discount, so the cart
/// shows it before paying. The checkout checks the code again.
/// </summary>
public class CheckShopPromoCodeEndpoint : Endpoint<ShopPromoCheckRequest, ShopPromoCheckResponse>
{
    private readonly ApexPerformanceContext _context;

    public CheckShopPromoCodeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/shop/promo-codes/check");
        AllowAnonymous();
        Options(x => x.WithTags("Shop").RequireRateLimiting(RateLimiting.ShopPromoCheck));
    }

    public override async Task HandleAsync(ShopPromoCheckRequest request, CancellationToken cancellationToken)
    {
        var (promo, error) = await ShopPromo.Find(_context, request.Code, cancellationToken);

        if (error is not null)
            ThrowError(error);

        var variantIds = request.Items.Select(x => x.VariantId).ToList();

        var variants = await _context.ShopProductVariants
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => variantIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var items = variants
            .Select(x => new ShopPromoItemResponse(x.Id, ShopPromo.UnitDiscount(promo!, x.ProductId, x.Product.Price)))
            .Where(x => x.UnitDiscount > 0)
            .ToList();

        if (items.Count == 0)
            ThrowError(ErrorCodes.PromoCodeNotApplicable);

        var discount = items.Sum(x => x.UnitDiscount * request.Items.First(i => i.VariantId == x.VariantId).Quantity);

        await SendAsync(new ShopPromoCheckResponse(promo!.Code, promo.DiscountType, promo.DiscountValue, discount, items),
            cancellation: cancellationToken);
    }
}

public sealed class ShopPromoCheckValidator : Validator<ShopPromoCheckRequest>
{
    public ShopPromoCheckValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(ErrorCodes.Required).MaximumLength(40).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Items).NotEmpty().WithMessage(ErrorCodes.Required)
            .Must(x => x.Count <= 30).WithMessage(ErrorCodes.NotValid)
            .Must(x => x.Select(i => i.VariantId).Distinct().Count() == x.Count).WithMessage(ErrorCodes.DuplicatesNotAllowed);
        RuleForEach(x => x.Items).ChildRules(item =>
            item.RuleFor(x => x.Quantity).InclusiveBetween(1, 20).WithMessage(ErrorCodes.NotValid));
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

        try
        {
            await _orderService.ConfirmAsync(id, cancellationToken);
        }
        catch (StripeException ex)
        {
            // The order is shown as pending, ShopOrderSyncJob confirms it a bit later.
            Logger.LogWarning(ex, "Shop order {OrderId} could not be checked with Stripe.", id);
        }

        var order = await _context.ShopOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == id, cancellationToken);

        await SendAsync(ShopOrders.ToResponse(order), cancellation: cancellationToken);
    }
}
