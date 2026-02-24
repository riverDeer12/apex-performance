namespace ApexPerformance.API.Database.Entities;

public class RecipeIngredient
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public Guid IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    public decimal? Quantity { get; set; }     // null = "to taste" / "as needed"
    
    public Guid? MeasurementUnitId { get; set; }
    public MeasurementUnit? MeasurementUnit { get; set; }
}