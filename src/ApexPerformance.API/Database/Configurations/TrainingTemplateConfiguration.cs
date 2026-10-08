using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class TrainingTemplateConfiguration : IEntityTypeConfiguration<TrainingTemplate>
{
    public void Configure(EntityTypeBuilder<TrainingTemplate> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);

        builder.Property(e => e.Note).HasMaxLength(2000);

        builder.HasMany(e => e.Exercises)
            .WithOne(e => e.TrainingTemplate)
            .HasForeignKey(e => e.TrainingTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("TrainingTemplates");
    }
}

public class TrainingTemplateExerciseConfiguration : IEntityTypeConfiguration<TrainingTemplateExercise>
{
    public void Configure(EntityTypeBuilder<TrainingTemplateExercise> builder)
    {
        builder.HasQueryFilter(x => !x.TrainingTemplate.IsDeleted);

        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasMany(e => e.Sets)
            .WithOne(e => e.TrainingTemplateExercise)
            .HasForeignKey(e => e.TrainingTemplateExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Workout)
            .WithMany()
            .HasForeignKey(e => e.WorkoutId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("TrainingTemplateExercises");
    }
}

public class TrainingTemplateExerciseSetConfiguration : IEntityTypeConfiguration<TrainingTemplateExerciseSet>
{
    public void Configure(EntityTypeBuilder<TrainingTemplateExerciseSet> builder)
    {
        builder.HasQueryFilter(x => !x.TrainingTemplateExercise.TrainingTemplate.IsDeleted);

        builder.Property(e => e.Reps).HasMaxLength(50);

        builder.Property(e => e.Weight).HasColumnType("decimal(6,2)");

        builder.ToTable("TrainingTemplateExerciseSets");
    }
}
