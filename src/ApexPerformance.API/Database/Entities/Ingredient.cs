using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class Ingredient : BaseEntity
{
    public string Name { get; set; }
    public decimal Calories { get; set; }
    public ICollection<RecipeIngredient> Recipes { get; set; }
}