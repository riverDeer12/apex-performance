using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.Property(e => e.Name).HasMaxLength(200);
        
        builder.Property(e => e.PreparationMinutes).HasColumnType("decimal(5,2)");
        
        builder.Property(e => e.CookingMinutes).HasColumnType("decimal(5,2)");
        
        builder.ToTable("Recipes", c => c.IsTemporal());
    }
}