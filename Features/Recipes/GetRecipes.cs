using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Ingredients;
using ApexPerformance.API.Features.MeasurementUnits;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Recipes;

public record GetRecipeResponse(
    LocalizedProperty Name,
    LocalizedProperty Content,
    decimal PreparationMinutes,
    decimal CookingMinutes,
    List<GetIngredientResponse> Ingredients
);

public class GetRecipesEndpoint : EndpointWithoutRequest<List<GetRecipeResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRecipesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/recipes");
        Options(x => x.WithTags("Recipes"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var recipes = await GetRecipesForUser(cancellationToken);

        if (recipes.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(
            recipes.Select(recipe =>
                new GetRecipeResponse(
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
                )
            ).ToList(),
            cancellation: cancellationToken
        );
    }

    private async Task<List<Recipe>> GetRecipesForUser(CancellationToken cancellationToken)
    {
        if (_currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Administrator))
            return await GetAllRecipes(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Client))
            return await GetClientRecipes(cancellationToken);

        return [];
    }

    private async Task<List<Recipe>> GetAllRecipes(CancellationToken cancellationToken)
    {
        return await _context.Recipes
            .AsNoTracking()
            .Include(r => r.Ingredients)
            .ThenInclude(recipeIngredient => recipeIngredient.Ingredient)
            .Include(r => r.Ingredients)
            .ThenInclude(recipeIngredient => recipeIngredient.MeasurementUnit)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Recipe>> GetClientRecipes(CancellationToken cancellationToken)
    {
        return await _context.Recipes
            .AsNoTracking()
            .Include(r => r.Ingredients)
            .ThenInclude(recipeIngredient => recipeIngredient.Ingredient)
            .Include(r => r.Ingredients)
            .ThenInclude(recipeIngredient => recipeIngredient.MeasurementUnit)
            .Where(x => x.CreatedBy == _currentUserService.UserId)
            .ToListAsync(cancellationToken);
    }
}