using ApexPerformance.API.Shared.Localization;

namespace ApexPerformance.API.Services.Interfaces;

public interface IRecipeService
{
    /// <summary>
    /// Check for duplicate of recipe.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="excRecipeId"></param>
    /// <returns></returns>
    bool RecipeExists(LocalizedProperty name, Guid? excRecipeId = null);
}