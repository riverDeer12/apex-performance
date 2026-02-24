using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Ingredients;

public class DeleteIngredientEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteIngredientEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/ingredients/{id}");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Ingredients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var ingredientId = Route<Guid>("id", isRequired: true);

        var ingredient =
            await _context.Ingredients
                .FirstOrDefaultAsync(x => x.Id == ingredientId, 
                    cancellationToken: cancellationToken);

        if (ingredient is null)
            ThrowError(ErrorCodes.NotFound);

        ingredient.Delete();

        _context.Ingredients.Update(ingredient);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new StatusResponse(ingredient.Id, true), cancellation: cancellationToken);
    }
}