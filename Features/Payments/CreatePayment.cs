using ApexPerformance.API.Constants;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using FluentValidation;
using Stripe.Checkout;

namespace ApexPerformance.API.Features.Payments;

public record CreatePaymentRequest(List<CheckoutItemDto> Items);

public record CreatePaymentResponse(Session session);

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
                    UnitAmount = (long)(item.Price * 100)
                },
                Quantity = item.Quantity
            }).ToList(),
            
            Mode = "payment",
            
            SuccessUrl = _configuration["Stripe::SuccessUrl"],
            
            CancelUrl = _configuration["Stripe::CancelUrl"]
        };

        var service = new SessionService();

        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        await SendAsync(new CreatePaymentResponse(session), cancellation: cancellationToken);
    }
}

public sealed class CreatePaymentRequestValidator : Validator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}