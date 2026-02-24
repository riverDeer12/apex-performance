using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.HasKey(bc => new { bc.RecipeId, bc.IngredientId });
        
        builder
            .HasOne(bc => bc.Recipe)
            .WithMany(b => b.Ingredients)
            .HasForeignKey(bc => bc.RecipeId);

        builder
            .HasOne(bc => bc.Ingredient)
            .WithMany(c => c.Recipes)
            .HasForeignKey(bc => bc.IngredientId);
        
        builder.HasOne(x => x.MeasurementUnit)
            .WithMany()
            .HasForeignKey(x => x.MeasurementUnitId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(e => e.Quantity).HasColumnType("decimal(5,2)");
    }
}