using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Products;

public class GetProductsEndpoint : EndpointWithoutRequest<List<ProductDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetProductsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/products");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("Products"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var products = await _context.Products.ToListAsync(cancellationToken: cancellationToken);

        if (products.Count is 0)
        {
            await SendNoContentAsync(cancellation: cancellationToken);
            return;
        }

        await SendAsync(products.Select(x => 
                new ProductDto(x.Name, x.Description, x.Price)).ToList(),
            cancellation: cancellationToken);
    }
}