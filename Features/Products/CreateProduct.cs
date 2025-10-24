using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Products;

public record CreateProductRequest(List<IFormFile> Photos, string Name, string Description, decimal Price);

public record CreateProductResponse(Guid Id);

public class CreateProductEndpoint : Endpoint<CreateProductRequest, CreateProductResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateProductEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/products");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("Products"));
    }

    public override async Task HandleAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var newProduct = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            IsActive = true
        };

        _context.Products.Add(newProduct);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception(ErrorMessages.SavingError);

        await SendAsync(new CreateProductResponse(newProduct.Id), cancellation: cancellationToken);
    }
}

public sealed class CreateProductRequestValidator : Validator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Photos).NotEmpty().WithMessage(ValidationMessages.Required);

        RuleForEach(x => x.Photos!).ChildRules(file =>
        {
            file.RuleFor(f => f.ContentType).Must(ct => ct.StartsWith("image/"))
                .WithMessage("Only image uploads are allowed.");
            file.RuleFor(f => f.Length).LessThanOrEqualTo(1024 * 1024)
                .WithMessage("Max size is 1MB.");
        });

        RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Description).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Price).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}