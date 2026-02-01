using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.Localization;

namespace ApexPerformance.API.Services.Implementation;

public class RecipeService : IRecipeService
{
    private readonly ApexPerformanceContext _context;

    public RecipeService(ApexPerformanceContext context)
    {
        _context = context;
    }

    public bool RecipeExists(LocalizedProperty name, Guid? excludeRecipeId = null)
    {
        return _context.Recipes
            .AsEnumerable()
            .Where(i => excludeRecipeId == null || i.Id != excludeRecipeId.Value)
            .Any(existing =>
            {
                var existingName = new LocalizedProperty(existing.Name);

                return existingName.Translations.Any(et =>
                    name.Translations.Any(nt =>
                        et.Key == nt.Key &&
                        string.Equals(et.Value, nt.Value, StringComparison.OrdinalIgnoreCase)
                    )
                );
            });
    }
}