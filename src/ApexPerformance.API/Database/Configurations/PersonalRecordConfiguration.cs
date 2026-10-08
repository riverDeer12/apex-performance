using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class PersonalRecordConfiguration : IEntityTypeConfiguration<PersonalRecord>
{
    public void Configure(EntityTypeBuilder<PersonalRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type).HasMaxLength(32);

        builder.Property(e => e.Value).HasColumnType("decimal(8,2)");

        builder.Property(e => e.Weight).HasColumnType("decimal(6,2)");

        builder.Property(e => e.Reps).HasColumnType("decimal(6,2)");

        // Records are rebuilt from trainings, so they are removed
        // by the recalculation, never by the database.
        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Workout)
            .WithMany()
            .HasForeignKey(e => e.WorkoutId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Training)
            .WithMany()
            .HasForeignKey(e => e.TrainingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ClientId, e.WorkoutId, e.AchievedAt });

        builder.ToTable("PersonalRecords");
    }
}
