using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class WorkoutWorkoutTypeConfiguration : IEntityTypeConfiguration<WorkoutWorkoutType>
{
    public void Configure(EntityTypeBuilder<WorkoutWorkoutType> builder)
    {
        builder.HasKey(bc => new { bc.WorkoutId, bc.WorkoutTypeId });

        builder
            .HasOne(bc => bc.Workout)
            .WithMany(b => b.WorkoutTypes)
            .HasForeignKey(bc => bc.WorkoutId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(bc => bc.WorkoutType)
            .WithMany(c => c.Workouts)
            .HasForeignKey(bc => bc.WorkoutTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}