using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.PriceLists;

public record PriceListProductDto(
    string StripeProductId,
    string Code,
    string Brand,
    decimal NetQuantity,
    string UnitOfMeasure,
    string? Barcode,
    string Category,
    decimal AnchorPrice);

public record SetPriceListProductsRequest(List<PriceListProductDto> Products);

/// <summary>
/// Products in the price list with their anchor prices.
/// </summary>
public class GetPriceListProductsEndpoint : EndpointWithoutRequest<List<PriceListProductDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetPriceListProductsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/price-lists/reviv-plus/products");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("PriceLists"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var products = await _context.PriceListProducts
            .OrderBy(x => x.SortOrder)
            .Select(x => new PriceListProductDto(x.StripeProductId, x.Code, x.Brand, x.NetQuantity,
                x.UnitOfMeasure, x.Barcode, x.Category, x.AnchorPrice))
            .ToListAsync(cancellationToken);

        await SendAsync(products, cancellation: cancellationToken);
    }
}

public class SetPriceListProductsValidator : Validator<SetPriceListProductsRequest>
{
    public SetPriceListProductsValidator()
    {
        RuleFor(x => x.Products).NotEmpty();

        RuleForEach(x => x.Products).ChildRules(product =>
        {
            product.RuleFor(x => x.StripeProductId).NotEmpty();
            product.RuleFor(x => x.Code).NotEmpty();
            product.RuleFor(x => x.Brand).NotEmpty();
            product.RuleFor(x => x.UnitOfMeasure).NotEmpty();
            product.RuleFor(x => x.Category).NotEmpty();
            product.RuleFor(x => x.NetQuantity).GreaterThan(0);
            product.RuleFor(x => x.AnchorPrice).GreaterThan(0);
        });

        RuleFor(x => x.Products)
            .Must(products => products.Select(x => x.StripeProductId).Distinct().Count() == products.Count)
            .WithMessage("Each Stripe product can be listed only once.");
    }
}

/// <summary>
/// Replace products in the price list. Order of products
/// in the request is the order of rows in the price list.
/// </summary>
public class SetPriceListProductsEndpoint : Endpoint<SetPriceListProductsRequest, List<PriceListProductDto>>
{
    private readonly ApexPerformanceContext _context;

    public SetPriceListProductsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/price-lists/reviv-plus/products");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("PriceLists"));
    }

    public override async Task HandleAsync(SetPriceListProductsRequest request, CancellationToken cancellationToken)
    {
        var existingProducts = await _context.PriceListProducts.ToListAsync(cancellationToken);

        foreach (var product in existingProducts)
            product.Delete();

        _context.PriceListProducts.AddRange(request.Products.Select((x, index) => new PriceListProduct
        {
            StripeProductId = x.StripeProductId,
            Code = x.Code,
            Brand = x.Brand,
            NetQuantity = x.NetQuantity,
            UnitOfMeasure = x.UnitOfMeasure,
            Barcode = x.Barcode,
            Category = x.Category,
            AnchorPrice = x.AnchorPrice,
            SortOrder = index
        }));

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(request.Products, cancellation: cancellationToken);
    }
}
