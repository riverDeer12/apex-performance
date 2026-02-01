using ApexPerformance.API.Shared.Localization;

namespace ApexPerformance.API.Services.Interfaces;

public interface IIngredientService
{
    /// <summary>
    /// Check for duplicate of ingredient.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="excludeIngredientId"></param>
    /// <returns></returns>
    bool IngredientExists(LocalizedProperty name, Guid? excludeIngredientId = null);
}