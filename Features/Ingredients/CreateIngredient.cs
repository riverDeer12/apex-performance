using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.Localization;
using FastEndpoints;

namespace ApexPerformance.API.Features.Ingredients;

public record CreateIngredientRequest(
    LocalizedProperty Name,
    decimal Calories
);

public class CreateIngredientEndpoint : Endpoint<CreateIngredientRequest, GetIngredientResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IIngredientService _ingredientService;

    public CreateIngredientEndpoint(ApexPerformanceContext context, IIngredientService ingredientService)
    {
        _context = context;
        _ingredientService = ingredientService;
    }

    public override void Configure()
    {
        Post("api/ingredients");
        Options(x => x.WithTags("Ingredients"));
    }

    public override async Task HandleAsync(CreateIngredientRequest request, CancellationToken cancellationToken)
    {
        if (_ingredientService.IngredientExists(request.Name))
            ThrowError(ErrorCodes.DuplicatesNotAllowed);

        var ingredient = new Ingredient
        {
            Name = request.Name.ToJsonString(),
            Calories = request.Calories
        };

        _context.Ingredients.Add(ingredient);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(
            new GetIngredientResponse(ingredient.Id, new LocalizedProperty(ingredient.Name), ingredient.Calories),
            cancellation: cancellationToken);
    }
}