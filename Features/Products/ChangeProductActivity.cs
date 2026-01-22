using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Products;

public class ChangeProductActivityEndpoint : EndpointWithoutRequest<int>
{
    private readonly ApexPerformanceContext _context;

    public ChangeProductActivityEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/products/{id}/activity");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("Products"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var productId = Route<Guid>("id", isRequired: true);

        var product =
            await _context.Products
                .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken: cancellationToken);

        if (product is null)
            ThrowError(ErrorCodes.NotFound);

        product.IsActive = !product.IsActive;
        
        _context.Products.Update(product);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception(ErrorCodes.SavingError);

        await SendAsync(StatusCodes.Status200OK, cancellation: cancellationToken);
    }
}