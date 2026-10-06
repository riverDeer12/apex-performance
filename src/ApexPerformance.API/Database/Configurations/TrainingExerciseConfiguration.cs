using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class TrainingExerciseConfiguration : IEntityTypeConfiguration<TrainingExercise>
{
    public void Configure(EntityTypeBuilder<TrainingExercise> builder)
    {
        // Same filter as training, so exercises of deleted
        // trainings are hidden together with them.
        builder.HasQueryFilter(x => !x.Training.IsDeleted);

        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasMany(e => e.Sets)
            .WithOne(e => e.TrainingExercise)
            .HasForeignKey(e => e.TrainingExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Workouts are soft deleted, so the relation is never removed by the database.
        builder.HasOne(e => e.Workout)
            .WithMany()
            .HasForeignKey(e => e.WorkoutId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("TrainingExercises");
    }
}
