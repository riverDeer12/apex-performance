using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities.Localization;

namespace ApexPerformance.API.Services.Implementation;

public class IngredientService : IIngredientService
{
    private readonly ApexPerformanceContext _context;

    public IngredientService(ApexPerformanceContext context)
    {
        _context = context;
    }

    public bool IngredientExists(LocalizedProperty name, Guid? excludeIngredientId = null)
    {
        return _context.Ingredients
            .AsEnumerable()
            .Where(i => excludeIngredientId == null || i.Id != excludeIngredientId.Value)
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