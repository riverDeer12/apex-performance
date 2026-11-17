using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class FavoriteWorkoutConfiguration : IEntityTypeConfiguration<FavoriteWorkout>
{
    public void Configure(EntityTypeBuilder<FavoriteWorkout> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Workouts are soft deleted, so a deleted workout's favorites stay in the
        // table but are left out by the filter (and by the workouts' own filter).
        builder.HasOne(e => e.Workout)
            .WithMany()
            .HasForeignKey(e => e.WorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => !e.Workout.IsDeleted);

        builder.HasIndex(e => new { e.UserId, e.WorkoutId }).IsUnique();

        builder.ToTable("FavoriteWorkouts");
    }
}
