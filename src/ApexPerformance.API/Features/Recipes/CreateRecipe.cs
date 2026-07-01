using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Ingredients;
using ApexPerformance.API.Features.MeasurementUnits;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Recipes;

public record CreateRecipeRequest(
    LocalizedProperty Name,
    LocalizedProperty Content,
    decimal PreparationMinutes,
    decimal CookingMinutes,
    List<RecipeIngredientRequest> Ingredients
);

public record RecipeIngredientRequest(
    Guid MeasurementUnit,
    Guid Ingredient,
    decimal Quantity
);

public class CreateRecipeEndpoint : Endpoint<CreateRecipeRequest, GetRecipeResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IRecipeService _recipeService;

    public CreateRecipeEndpoint(ApexPerformanceContext context, IRecipeService recipeService)
    {
        _context = context;
        _recipeService = recipeService;
    }

    public override void Configure()
    {
        Post("api/recipes");
        Options(x => x.WithTags("Recipes"));
    }

    public override async Task HandleAsync(CreateRecipeRequest request, CancellationToken cancellationToken)
    {
        if(_recipeService.RecipeExists(request.Name))
            ThrowError(ErrorCodes.DuplicatesNotAllowed);
        
        var recipe = new Recipe
        {
            Name = request.Name.ToJsonString(),
            Content = request.Content.ToJsonString(),
            PreparationMinutes = request.PreparationMinutes,
            CookingMinutes = request.CookingMinutes,
            Ingredients = request.Ingredients.Select(x => new RecipeIngredient
            {
                IngredientId = x.Ingredient,
                MeasurementUnitId = x.MeasurementUnit,
                Quantity = x.Quantity
            }).ToList()
        };

        _context.Recipes.Add(recipe);

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

public sealed class CreateRecipeValidator : Validator<CreateRecipeRequest>
{
    public CreateRecipeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required);

        RuleFor(x => x.Content).NotEmpty().WithMessage(ErrorCodes.Required);

        RuleFor(x => x.PreparationMinutes).NotEmpty().WithMessage(ErrorCodes.Required);

        RuleFor(x => x.CookingMinutes).NotEmpty().WithMessage(ErrorCodes.Required);

        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage(ErrorCodes.DuplicatesNotAllowed)
            .MustAsync(async (recipeIngredients, cancellationToken) =>
            {
                var db = Resolve<ApexPerformanceContext>();

                var ingredientIds = recipeIngredients
                    .Select(x => x.Ingredient).ToList();
                
                var numberOfIngredients = await db.Ingredients
                    .Where(ingredient => ingredientIds.Contains(ingredient.Id))
                    .CountAsync(cancellationToken);

                var validIngredients = numberOfIngredients == ingredientIds.Count;
                
                return validIngredients;
            })
            .WithMessage(ErrorCodes.NotFound);
    }
}