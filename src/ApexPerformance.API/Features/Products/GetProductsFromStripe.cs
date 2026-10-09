using ApexPerformance.API.Features.Payments;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Stripe;

namespace ApexPerformance.API.Features.Products;

/// <param name="AnchorPrice">Price on the anchor date (ReViv Plus price list), null for other products.</param>
/// <param name="AnchorDate">Date of the anchor price, e.g. "10.9.2026.".</param>
public record StripeProduct(string ProductId, string PriceId, string Name, string Price,
    string? AnchorPrice = null, string? AnchorDate = null);

public record GetProductsFromStripeRequest(List<string> Products);

public class GetProductsFromStripeEndpoint : Endpoint<GetProductsFromStripeRequest, List<StripeProduct>>
{
    private readonly IConfiguration _configuration;
    private readonly IPriceListService _priceListService;

    public GetProductsFromStripeEndpoint(IConfiguration configuration, IPriceListService priceListService)
    {
        _configuration = configuration;
        _priceListService = priceListService;
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

        var anchorPrices = await _priceListService.GetAnchorPricesAsync(cancellationToken);

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

            var stripeCurrency = "€";
            
            var formattedPrice = $"{productPrice.UnitAmountDecimal / 100:0.00} {stripeCurrency}";

            var hasAnchorPrice = anchorPrices.TryGetValue(productId, out var anchorPrice);

            var stripeProduct = new StripeProduct
            (
                productId,
                product.DefaultPriceId,
                product.Name,
                formattedPrice,
                hasAnchorPrice ? $"{anchorPrice:0.00} {stripeCurrency}" : null,
                hasAnchorPrice ? _priceListService.AnchorDate : null
            );

            stripeProducts.Add(stripeProduct);
        }

        await SendAsync(stripeProducts, cancellation: cancellationToken);
    }
}