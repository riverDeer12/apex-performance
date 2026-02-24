using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.Property(e => e.Name).HasMaxLength(200);
        
        builder.Property(e => e.Calories).HasColumnType("decimal(5,2)");
        
        builder.ToTable("Ingredients", c => c.IsTemporal());
    }
}