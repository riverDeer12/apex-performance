using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Ingredients;
using ApexPerformance.API.Features.MeasurementUnits;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Recipes;

public record UpdateRecipeRequest(
    LocalizedProperty Name,
    LocalizedProperty Content,
    decimal PreparationMinutes,
    decimal CookingMinutes,
    List<RecipeIngredientRequest> Ingredients
);

public class UpdateRecipeEndpoint : Endpoint<UpdateRecipeRequest, GetRecipeResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IRecipeService _recipeService;
    
    public UpdateRecipeEndpoint(ApexPerformanceContext context, IRecipeService recipeService)
    {
        _context = context;
        _recipeService = recipeService;
    }

    public override void Configure()
    {
        Put("api/recipes/{id}");
        Options(x => x.WithTags("Recipes"));
    }

    public override async Task HandleAsync(UpdateRecipeRequest request, CancellationToken cancellationToken)
    {
        var recipeId = Route<Guid>("id", isRequired: true);

        var recipe =
            await _context.Recipes.Include(recipe => recipe.Ingredients)
                .ThenInclude(recipeIngredient => recipeIngredient.Ingredient)
                .Include(recipe => recipe.Ingredients)
                .ThenInclude(recipeIngredient => recipeIngredient.MeasurementUnit)
                .FirstOrDefaultAsync(x => x.Id == recipeId, cancellationToken: cancellationToken);

        if (recipe is null)
            ThrowError(ErrorCodes.NotFound);
        
        if(_recipeService.RecipeExists(request.Name, recipeId))
            ThrowError(ErrorCodes.DuplicatesNotAllowed);

        recipe.Name = request.Name.ToJsonString();
        recipe.Content = request.Content.ToJsonString();
        recipe.PreparationMinutes = request.PreparationMinutes;
        recipe.CookingMinutes = request.CookingMinutes;

        recipe.Ingredients.Clear();

        foreach (var x in request.Ingredients)
        {
            recipe.Ingredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.Id,
                IngredientId = x.Ingredient,
                MeasurementUnitId = x.MeasurementUnit,
                Quantity = x.Quantity
            });
        }

        _context.Recipes.Update(recipe);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        var recipeResponse = await _context.Recipes
            .Include(recipe => recipe.Ingredients)
            .ThenInclude(recipeIngredient => recipeIngredient.Ingredient)
            .Include(recipe => recipe.Ingredients)
            .ThenInclude(recipeIngredient => recipeIngredient.MeasurementUnit)
            .FirstOrDefaultAsync(x => x.Id == recipe.Id, cancellationToken: cancellationToken);
        
        if(recipeResponse is null)
            ThrowError(ErrorCodes.NotFound);

        await SendAsync(new GetRecipeResponse(
            new LocalizedProperty(recipeResponse.Name),
            new LocalizedProperty(recipeResponse.Content),
            recipeResponse.PreparationMinutes,
            recipeResponse.CookingMinutes,
            recipeResponse.Ingredients
                .Select(recipeIngredient =>
                    new GetIngredientResponse(
                        recipeIngredient.Ingredient.Id,
                        new LocalizedProperty(recipeIngredient.Ingredient.Name),
                        recipeIngredient.Ingredient.Calories,
                        new GetMeasurementUnitResponse(
                            recipeIngredient.MeasurementUnit.Id,
                            new LocalizedProperty(recipeIngredient.MeasurementUnit?.Name),
                            recipeIngredient.MeasurementUnit.Symbol),
                        recipeIngredient.Quantity
                    )
                )
                .ToList()
        ), cancellation: cancellationToken);
    }
}