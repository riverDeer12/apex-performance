using FastEndpoints;
using Stripe;
using Stripe.Checkout;

namespace ApexPerformance.API.Features.Payments;

public record ReVivPlusCheckoutItemDto(string PriceId, int Quantity);

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
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],

            LineItems = request.Items.Select(item => new SessionLineItemOptions
            {
                Price = item.PriceId,
                Quantity = item.Quantity,
            }).ToList(),

            BillingAddressCollection = "required",

            Mode = "payment",

            SuccessUrl = _configuration["Stripe:ReVivPlus:SuccessUrl"],

            CancelUrl = _configuration["Stripe:ReVivPlus:SuccessUrl"],
        };

        var service = new SessionService();

        var session = await service.CreateAsync(options,
            requestOptions: new RequestOptions { ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"] },
            cancellationToken: cancellationToken);

        await SendAsync(new CreatePaymentResponse(session.Id), cancellation: cancellationToken);
    }
}