using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Ingredients;

public record UpdateIngredientRequest(
    LocalizedProperty Name,
    decimal Calories
);

public class UpdateIngredientEndpoint : Endpoint<UpdateIngredientRequest, GetIngredientResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IIngredientService _ingredientService;

    public UpdateIngredientEndpoint(ApexPerformanceContext context, IIngredientService ingredientService)
    {
        _context = context;
        _ingredientService = ingredientService;
    }

    public override void Configure()
    {
        Put("api/ingredients/{id}");
        Options(x => x.WithTags("Ingredients"));
    }

    public override async Task HandleAsync(UpdateIngredientRequest request, CancellationToken cancellationToken)
    {
        var ingredientId = Route<Guid>("id", isRequired: true);

        var ingredient =
            await _context.Ingredients
                .FirstOrDefaultAsync(x => x.Id == ingredientId, cancellationToken: cancellationToken);

        if (ingredient is null)
            ThrowError(ErrorCodes.NotFound);

        if (_ingredientService.IngredientExists(request.Name, ingredientId))
            ThrowError(ErrorCodes.DuplicatesNotAllowed);

        ingredient.Name = request.Name.ToJsonString();
        ingredient.Calories = request.Calories;

        _context.Ingredients.Update(ingredient);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(
            new GetIngredientResponse(ingredient.Id, new LocalizedProperty(ingredient.Name), ingredient.Calories),
            cancellation: cancellationToken);
    }
}