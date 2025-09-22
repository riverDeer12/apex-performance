using ApexPerformance.API.Constants;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using FluentValidation;
using Stripe;
using Stripe.Checkout;

namespace ApexPerformance.API.Features.Payments;

public record CreatePaymentRequest(
    PersonDataDto Customer,
    List<CheckoutItemDto> Items);

public record CreatePaymentResponse(string SessionId);

public class CreatePaymentEndpoint : Endpoint<CreatePaymentRequest, CreatePaymentResponse>
{
    private readonly IConfiguration _configuration;

    public CreatePaymentEndpoint(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("api/payments");
        AllowAnonymous();
        Options(x => x.WithTags("Payments"));
    }

    public override async Task HandleAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],

            LineItems = request.Items.Select(item => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "eur",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Name
                    },
                    UnitAmount = (long)(item.Price * item.Quantity * 100)
                },
                Quantity = item.Quantity
            }).ToList(),
            
            BillingAddressCollection = "required",

            Mode = "payment",

            SuccessUrl = _configuration["Stripe:ApexPerformance:SuccessUrl"],

            CancelUrl = _configuration["Stripe:ApexPerformance:CancelUrl"]
        };

        var service = new SessionService();

        var session = await service.CreateAsync(options,
            requestOptions: new RequestOptions { ApiKey = _configuration["Stripe:ApexPerformance:SecretKey"] },
            cancellationToken: cancellationToken);
        
        await SendAsync(new CreatePaymentResponse(session.Id), cancellation: cancellationToken);
    }
}

public sealed class CreatePaymentRequestValidator : Validator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.Customer).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Items).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}