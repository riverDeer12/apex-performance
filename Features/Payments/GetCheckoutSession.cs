using FastEndpoints;
using Stripe;
using Stripe.Checkout;

namespace ApexPerformance.API.Features.Payments;

public record GetCheckoutSessionResponse(
    string SessionId,
    string Status,
    string PaymentStatus,
    string Currency,
    long? AmountSubtotal,
    long? AmountTotal,
    long? TaxTotal,
    long? DiscountTotal,
    long? ShippingTotal,
    string ShippingTitle,
    string CustomerEmail,
    string CustomerName,
    List<LineItem> LineItems
);

public record LineItem(
    string Description,
    long Quantity,
    string Currency,
    long? UnitAmount,
    long? AmountSubtotal,
    long? AmountTotal
);

public class GetCheckoutSessionEndpoint : EndpointWithoutRequest<GetCheckoutSessionResponse>
{
    private readonly IConfiguration _configuration;

    public GetCheckoutSessionEndpoint(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void Configure()
    {
        Get("api/payments/checkout-session/{sessionId}");
        AllowAnonymous();
        Options(x => x.WithTags("Payments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var sessionId = Route<string>("sessionId", isRequired: true);

        var sessionService = new SessionService();

        var session = await sessionService.GetAsync(sessionId, new SessionGetOptions
            {
                Expand = new List<string>
                {
                    "payment_intent",
                    "total_details.breakdown",
                    "customer",
                    "shipping_cost.shipping_rate"
                },
            },
            requestOptions: new RequestOptions
            {
                ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"]
            },
            cancellationToken: cancellationToken);

        // Pull line items (names, quantities, amounts, etc.)
        var lineItemService = new SessionLineItemService();

        var lineItems = await lineItemService.ListAsync(sessionId, new SessionLineItemListOptions
            {
                Limit = 100,
                Expand = new List<string> { "data.price.product" }
            },
            requestOptions: new RequestOptions
            {
                ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"]
            },
            cancellationToken: cancellationToken);

        // Build DTO
        var items = lineItems.Data.Select(li => new LineItem
        (
            li.Description ?? li.Price?.Nickname ?? li.Price?.Product?.ToString(),
            li.Quantity ?? 0,
            li.Currency,
            li.Price?.UnitAmount, // in the smallest currency unit (e.g., cents)
            li.AmountSubtotal, // qty * unit - per-item discounts
            li.AmountTotal // after per-item discounts/tax
        )).ToList();

        var summary = new GetCheckoutSessionResponse
        (
             session.Id,
            session.Status, // "complete", "open", etc.
            session.PaymentStatus, // "paid", "unpaid", "no_payment_required"
            session.Currency,
           session.AmountSubtotal, // before shipping/tax/discounts
            session.AmountTotal, // final total charged/authorized
            session.TotalDetails?.AmountTax,
            session.TotalDetails?.AmountDiscount,
            session.ShippingCost?.AmountSubtotal ?? 0,
            session.ShippingCost?.ShippingRate?.DisplayName,
            session.CustomerDetails?.Email,
            session.CustomerDetails?.Name,
            items
        );
    }
}