using ApexPerformance.API.Features.Payments;
using FastEndpoints;
using Stripe;

namespace ApexPerformance.API.Features.Products;

public record StripeProduct(string ProductId, string PriceId, string Name, string Price);

public record GetProductsFromStripeRequest(List<string> Products);

public class GetProductsFromStripeEndpoint : Endpoint<GetProductsFromStripeRequest, List<StripeProduct>>
{
    private readonly IConfiguration _configuration;

    public GetProductsFromStripeEndpoint(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("api/products/stripe");
        AllowAnonymous();
        Options(x => x.WithTags("Products"));
    }

    public override async Task HandleAsync(GetProductsFromStripeRequest request, CancellationToken cancellationToken)
    {
        var stripeProducts = new List<StripeProduct>();

        var productService = new ProductService();

        var priceService = new PriceService();

        foreach (var productId in request.Products)
        {
            var product = await productService.GetAsync(productId, requestOptions: new RequestOptions
            {
                ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"]
            }, cancellationToken: cancellationToken);

            var productPrice =
                await priceService.GetAsync(product.DefaultPriceId, requestOptions: new RequestOptions
                {
                    ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"]
                }, cancellationToken: cancellationToken);

            var stripeProduct = new StripeProduct
            (
                productId,
                product.DefaultPriceId,
                product.Name,
                productPrice != null
                    ? $"{(productPrice.UnitAmountDecimal / 100):C}"
                    : "N/A"
            );

            stripeProducts.Add(stripeProduct);
        }

        await SendAsync(stripeProducts, cancellation: cancellationToken);
    }
}