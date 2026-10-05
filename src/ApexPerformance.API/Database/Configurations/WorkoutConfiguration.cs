using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class WorkoutConfiguration : IEntityTypeConfiguration<Workout>
{
    public void Configure(EntityTypeBuilder<Workout> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Contains JSON with name in all languages.
        builder.Property(e => e.Name).HasMaxLength(500);
        
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.ToTable("Workouts", c => c.IsTemporal());    
    }
}