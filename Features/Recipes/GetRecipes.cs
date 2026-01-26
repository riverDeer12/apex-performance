using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;

namespace ApexPerformance.API.Features.Recipes;

public record GetRecipeResponse();

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
        return [];
    }

    private async Task<List<Recipe>> GetClientRecipes(CancellationToken cancellationToken)
    {
        return [];
    }
}