using ApexPerformance.API.Features.Payments;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Stripe;

namespace ApexPerformance.API.Features.Products;

/// <param name="AnchorPrice">Price on the anchor date (ReViv Plus price list), null for other products.</param>
/// <param name="AnchorDate">Date of the anchor price, e.g. "10.9.2026.".</param>
/// <param name="LowestPriceLast30Days">Lowest price in the last 30 days (ReViv Plus price list), null for other products.</param>
public record StripeProduct(string ProductId, string PriceId, string Name, string Price,
    string? AnchorPrice = null, string? AnchorDate = null, string? LowestPriceLast30Days = null);

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

        var stripePrices = new List<(Product Product, decimal Price)>();

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

            stripePrices.Add((product, (productPrice.UnitAmountDecimal ?? 0) / 100));
        }

        // Only products in the price list have an anchor price and price history.
        var lowestPrices = await _priceListService.GetLowestPricesLast30DaysAsync(
            stripePrices
                .Where(x => anchorPrices.ContainsKey(x.Product.Id))
                .ToDictionary(x => x.Product.Id, x => x.Price),
            cancellationToken);

        var stripeCurrency = "€";

        foreach (var (product, price) in stripePrices)
        {
            var hasAnchorPrice = anchorPrices.TryGetValue(product.Id, out var anchorPrice);
            var hasLowestPrice = lowestPrices.TryGetValue(product.Id, out var lowestPrice);

            stripeProducts.Add(new StripeProduct
            (
                product.Id,
                product.DefaultPriceId,
                product.Name,
                $"{price:0.00} {stripeCurrency}",
                hasAnchorPrice ? $"{anchorPrice:0.00} {stripeCurrency}" : null,
                hasAnchorPrice ? _priceListService.AnchorDate : null,
                hasLowestPrice ? $"{lowestPrice:0.00} {stripeCurrency}" : null
            ));
        }

        await SendAsync(stripeProducts, cancellation: cancellationToken);
    }
}