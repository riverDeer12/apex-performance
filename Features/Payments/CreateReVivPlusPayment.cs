using FastEndpoints;
using Stripe;
using Stripe.Checkout;

namespace ApexPerformance.API.Features.Payments;

public record ReVivPlusCheckoutItemDto(string ProductId, int Quantity);

public record CreateReVivPlusPaymentRequest(
    List<ReVivPlusCheckoutItemDto> Items,
    string BoxNowLockerId,
    string BoxNowLockerAddressLine1,
    string BoxNowLockerPostalCode);

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
            CustomerCreation = "always",
            Mode = "payment",
            PhoneNumberCollection = new SessionPhoneNumberCollectionOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string?>
            {
                { "BoxNowLockerId", request.BoxNowLockerId },
                { "BoxNowLockerAddressLine1", request.BoxNowLockerAddressLine1 },
                { "BoxNowLockerPostalCode", request.BoxNowLockerPostalCode }
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string?>
                {
                    { "BoxNowLockerId", request.BoxNowLockerId },
                    { "BoxNowLockerAddressLine1", request.BoxNowLockerAddressLine1 },
                    { "BoxNowLockerPostalCode", request.BoxNowLockerPostalCode }
                }
            },
            AllowPromotionCodes = true,
            SuccessUrl = _configuration["Stripe:ReVivPlus:SuccessUrl"] + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = _configuration["Stripe:ReVivPlus:CancelUrl"] + "?session_id={CHECKOUT_SESSION_ID}",
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