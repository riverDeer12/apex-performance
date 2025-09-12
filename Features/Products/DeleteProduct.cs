using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Products;

public sealed record DeleteProductResponse(Guid Id);

public class DeleteProductEndpoint : EndpointWithoutRequest<DeleteProductResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteProductEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/products/{id}");
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
            ThrowError(ErrorMessages.NotFound);

        product.Delete();

        _context.Products.Update(product);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new DeleteProductResponse(product.Id), cancellation: cancellationToken);
    }
}