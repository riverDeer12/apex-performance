using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Products;

public record CreateProductRequest(
    IReadOnlyList<IFormFile> Photos,
    string Name,
    string Description,
    decimal Price);

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
        AllowFileUploads();
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

        // Ensure uploads folder exists
        var uploadsRoot = Path.Combine(AppContext.BaseDirectory, "Uploads", "Products", 
            newProduct.Id.ToString());

        Directory.CreateDirectory(uploadsRoot);

        // Save each photo & create ProductPhoto rows
        foreach (var formFile in request.Photos ?? [])
        {
            if (formFile.Length == 0) continue;

            // Extra safety: verify content-type & size server-side too
            if (!formFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                continue;

            var safeName = Path.GetFileName(formFile.FileName).Replace(' ', '_');
            var uniqueName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{safeName}";
            var filePath = Path.Combine(uploadsRoot, uniqueName);

            // Buffered save (IFormFile) – simple and fine for typical image sizes
            await using (var stream = File.Create(filePath))
            {
                await formFile.CopyToAsync(stream, cancellationToken);
            }

            var photo = new ProductImage()
            {
                ProductId = newProduct.Id,
                Product = newProduct,
                Name = uniqueName,
                Source = Path.Combine("Uploads", "Products", newProduct.Id.ToString(), uniqueName)
            };

            _context.ProductImages.Add(photo);
        }

        await _context.SaveChangesAsync(cancellationToken);

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