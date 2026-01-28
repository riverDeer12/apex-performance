using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Ingredients;
using ApexPerformance.API.Features.MeasurementUnits;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.Localization;
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
    Guid MeasurementUnitId,
    Guid IngredientId,
    decimal Quantity
);

public class CreateRecipeEndpoint : Endpoint<CreateRecipeRequest, GetRecipeResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateRecipeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/recipes");
        Options(x => x.WithTags("Recipes"));
    }

    public override async Task HandleAsync(CreateRecipeRequest request, CancellationToken cancellationToken)
    {
        var recipe = new Recipe
        {
            Name = request.Name.ToJsonString(),
            Content = request.Content.ToJsonString(),
            PreparationMinutes = request.PreparationMinutes,
            CookingMinutes = request.CookingMinutes,
            Ingredients = request.Ingredients.Select(x => new RecipeIngredient
            {
                IngredientId = x.IngredientId,
                MeasurementUnitId = x.MeasurementUnitId,
                Quantity = x.Quantity
            }).ToList()
        };

        _context.Recipes.Add(recipe);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new GetRecipeResponse(
            new LocalizedProperty(recipe.Name),
            new LocalizedProperty(recipe.Content),
            recipe.PreparationMinutes,
            recipe.CookingMinutes,
            recipe.Ingredients
                .Select(recipeIngredient =>
                    new GetIngredientResponse(
                        new LocalizedProperty(recipeIngredient.Ingredient.Name),
                        recipeIngredient.Ingredient.Calories,
                        new GetMeasurementUnitResponse(
                            new LocalizedProperty(recipeIngredient.MeasurementUnit.Name),
                            recipeIngredient.MeasurementUnit.Symbol)
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
                    .Select(x => x.IngredientId).ToList();

                var measurementUnitIds = recipeIngredients.Select(x => x.MeasurementUnitId).ToList();

                var numberOfIngredients = await db.Ingredients
                    .Where(ingredient => ingredientIds.Contains(ingredient.Id))
                    .CountAsync(cancellationToken);

                var numberOfMeasurementUnits = await db.MeasurementUnits
                    .Where(measurementUnit => measurementUnitIds.Contains(measurementUnit.Id))
                    .CountAsync(cancellationToken);

                var validIngredients = numberOfIngredients == ingredientIds.Count;

                var validMeasurementUnits = numberOfMeasurementUnits == measurementUnitIds.Count;

                return validMeasurementUnits && validIngredients;
            })
            .WithMessage(ErrorCodes.NotFound);
    }
}