using FastEndpoints;
using Stripe;
using Stripe.Checkout;

namespace ApexPerformance.API.Features.Payments;

public record GetCheckoutSessionResponse(
    string SessionId,
    string Status,
    string PaymentStatus,
    string Currency,
    string? AmountSubtotal,
    string? AmountTotal,
    string? TaxTotal,
    string? DiscountTotal,
    string? ShippingTotal,
    string ShippingTitle,
    string CustomerEmail,
    string CustomerName,
    Dictionary<string, string>? SessionMetadata,
    Dictionary<string, string>? PaymentIntentMetadata,
    Dictionary<string, string>? CustomerMetadata,
    List<LineItem> LineItems
);

public record LineItem(
    string Description,
    long Quantity,
    string Currency,
    string? UnitAmount,
    string? AmountSubtotal,
    string? AmountTotal
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
        
        var items = lineItems.Data.Select(li => new LineItem
        (
            li.Description ?? li.Price?.Nickname ?? li.Price?.Product?.ToString(),
            li.Quantity ?? 0,
            li.Currency,
            $"{(li.Price?.UnitAmount ?? 0) / 100m:0.00} €",
            $"{(li.AmountSubtotal) / 100m:0.00} €",
            $"{(li.AmountTotal) / 100m:0.00} €"
        )).ToList();

        var summary = new GetCheckoutSessionResponse
        (
            session.Id,
            session.Status,
            session.PaymentStatus,
            session.Currency,
            $"{(session.AmountSubtotal ?? 0) / 100m:0.00} €",
            $"{(session.AmountTotal ?? 0) / 100m:0.00} €",
            $"{(session.TotalDetails?.AmountTax ?? 0) / 100m:0.00} €",
            $"{(session.TotalDetails?.AmountDiscount ?? 0) / 100m:0.00} €",
            $"{(session.ShippingCost?.AmountSubtotal ?? 0) / 100m:0.00} €",
            session.ShippingCost?.ShippingRate?.DisplayName,
            session.CustomerDetails?.Email,
            session.CustomerDetails?.Name,
            session.PaymentIntent.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
            session.PaymentIntent?.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
            session.Customer?.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
            items
        );

        await SendAsync(summary, cancellation: cancellationToken);
    }
}