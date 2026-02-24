using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class Recipe : BaseEntity
{
    public string Name { get; set; }
    public string Content { get; set; }
    public decimal PreparationMinutes { get; set; }
    public decimal CookingMinutes { get; set; }
    public ICollection<RecipeIngredient> Ingredients { get; set; }
}