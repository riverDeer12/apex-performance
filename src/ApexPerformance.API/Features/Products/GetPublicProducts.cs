using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Products;

public class GetPublicProductsEndpoint : EndpointWithoutRequest<List<ProductDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetPublicProductsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/products/public");
        AllowAnonymous();
        Options(x => x.WithTags("Products"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken: cancellationToken);

        if (products.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(products.Select(x =>
                new ProductDto(x.Id, x.Name, x.Description, x.Price, x.IsActive)).ToList(),
            cancellation: cancellationToken);
    }
}