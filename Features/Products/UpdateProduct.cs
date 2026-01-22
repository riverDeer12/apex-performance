using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Products;

public record UpdateProductRequest(string Name, string Description, decimal Price);

public record UpdateProductResponse(Guid Id);

public class UpdateProductEndpoint : Endpoint<UpdateProductRequest, UpdateProductResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateProductEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/products/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("Products"));
    }

    public override async Task HandleAsync(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var productId = Route<Guid>("id", isRequired: true);

        var product =
            await _context.Products
                .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken: cancellationToken);

        if (product is null)
            ThrowError(ErrorCodes.NotFound);

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;

        _context.Products.Update(product);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception(ErrorCodes.SavingError);

        await SendAsync(new UpdateProductResponse(product.Id), cancellation: cancellationToken);
    }
}

public sealed class UpdateProductRequestValidator : Validator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Price).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}