using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class TrainingExerciseSetConfiguration : IEntityTypeConfiguration<TrainingExerciseSet>
{
    public void Configure(EntityTypeBuilder<TrainingExerciseSet> builder)
    {
        // Same filter as training, so sets of deleted
        // trainings are hidden together with them.
        builder.HasQueryFilter(x => !x.TrainingExercise.Training.IsDeleted);

        builder.Property(e => e.Reps).HasMaxLength(50);

        builder.Property(e => e.Weight).HasColumnType("decimal(6,2)");

        builder.ToTable("TrainingExerciseSets");
    }
}
