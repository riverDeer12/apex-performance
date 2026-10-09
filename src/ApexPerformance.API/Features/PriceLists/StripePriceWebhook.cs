using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Hangfire;
using Stripe;

namespace ApexPerformance.API.Features.PriceLists;

/// <summary>
/// Stripe calls this when the shop owner changes a product or price,
/// so the price list always matches the prices on the web shop.
/// Configure it in the ReViv Plus Stripe account for the events
/// product.updated, price.created and price.updated.
/// </summary>
public class StripePriceWebhookEndpoint : EndpointWithoutRequest
{
    private static readonly string[] PriceEvents = { "product.updated", "price.created", "price.updated" };

    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePriceWebhookEndpoint> _logger;

    public StripePriceWebhookEndpoint(IConfiguration configuration, ILogger<StripePriceWebhookEndpoint> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("api/price-lists/reviv-plus/stripe-webhook");
        AllowAnonymous();
        Options(x => x.WithTags("PriceLists"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(cancellationToken);

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(json,
                HttpContext.Request.Headers["Stripe-Signature"],
                _configuration["Stripe:ReVivPlus:PriceWebhookSecret"],
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException exception)
        {
            _logger.LogWarning(exception, "Invalid Stripe price webhook request.");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }

        // Stripe sends several events for one change (new price, then new default price),
        // the job publishes only when a price is really different from the last file.
        if (PriceEvents.Contains(stripeEvent.Type))
            BackgroundJob.Enqueue<IPriceListService>(service => service.PublishIfPricesChangedAsync());

        await SendOkAsync(cancellationToken);
    }
}
