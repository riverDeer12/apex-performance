using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;

namespace ApexPerformance.API.Features.Recipes;


public record UpdateRecipeRequest();

public class UpdateRecipeEndpoint : Endpoint<UpdateRecipeRequest, GetRecipeResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateRecipeEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/recipes/{id}");
        Options(x => x.WithTags("Recipes"));
    }
}