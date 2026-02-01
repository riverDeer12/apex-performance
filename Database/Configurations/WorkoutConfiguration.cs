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
        
        builder.Property(e => e.Name).HasMaxLength(200);
        
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder
            .HasOne(a => a.WorkoutType)
            .WithMany(b => b.Workouts)
            .HasForeignKey(b => b.WorkoutTypeId);
        
        builder.ToTable("Workouts", c => c.IsTemporal());    
    }
}