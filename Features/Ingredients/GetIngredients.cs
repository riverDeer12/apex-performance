using ApexPerformance.API.Database;
using ApexPerformance.API.Features.MeasurementUnits;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Ingredients;

public record GetIngredientResponse(
    Guid Id,
    LocalizedProperty Name,
    decimal Calories,
    GetMeasurementUnitResponse? MeasurementUnit = null,
    decimal? Quantity = null
);

public class GetIngredientsEndpoint : EndpointWithoutRequest<List<GetIngredientResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetIngredientsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/ingredients");
        Options(x => x.WithTags("Ingredients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var ingredients = await _context.Ingredients
            .ToListAsync(cancellationToken: cancellationToken);

        if (ingredients.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(
            ingredients.Select(ingredient =>
                new GetIngredientResponse(
                    ingredient.Id,
                    new LocalizedProperty(ingredient.Name),
                    ingredient.Calories,
                    null
                )
            ).ToList(),
            cancellation: cancellationToken
        );
    }
}