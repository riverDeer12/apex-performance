using FastEndpoints;
using Stripe;
using Stripe.Checkout;

namespace ApexPerformance.API.Features.Payments;

public record ReVivPlusCheckoutItemDto(string ProductId, int Quantity);

public record CreateReVivPlusPaymentRequest(List<ReVivPlusCheckoutItemDto> Items);

public class CreateReVivPlusPayment : Endpoint<CreateReVivPlusPaymentRequest, CreatePaymentResponse>
{
    private readonly IConfiguration _configuration;

    public CreateReVivPlusPayment(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("api/payments/reviv-plus");
        AllowAnonymous();
        Options(x => x.WithTags("Payments"));
    }

    public override async Task HandleAsync(CreateReVivPlusPaymentRequest request, CancellationToken cancellationToken)
    {
        var productService = new ProductService();
        var lineItems = new List<SessionLineItemOptions>();

        foreach (var item in request.Items)
        {
            var product = await productService.GetAsync(item.ProductId, requestOptions: new RequestOptions
            {
                ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"]
            }, cancellationToken: cancellationToken);

            var priceId = product.DefaultPriceId;
            if (string.IsNullOrEmpty(priceId))
                throw new InvalidOperationException($"Product {item.ProductId} has no default price set.");

            lineItems.Add(new SessionLineItemOptions
            {
                Price = priceId,
                Quantity = item.Quantity
            });
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            BillingAddressCollection = "required",
            Mode = "payment",
            SuccessUrl = _configuration["Stripe:ReVivPlus:SuccessUrl"],
            CancelUrl  = _configuration["Stripe:ReVivPlus:CancelUrl"], // use a real cancel URL
        };

        var service = new SessionService();

        var session = await service.CreateAsync(
            options,
            requestOptions: new RequestOptions { ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"] },
            cancellationToken: cancellationToken
        );

        await SendAsync(new CreatePaymentResponse(session.Id), cancellation: cancellationToken);
    }
}