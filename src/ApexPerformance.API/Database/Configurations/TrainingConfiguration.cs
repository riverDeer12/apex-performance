using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class TrainingConfiguration : IEntityTypeConfiguration<Training>
{
    public void Configure(EntityTypeBuilder<Training> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);

        builder.Property(e => e.Note).HasMaxLength(2000);

        builder.HasMany(e => e.Exercises)
            .WithOne(e => e.Training)
            .HasForeignKey(e => e.TrainingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Trainings", c => c.IsTemporal());
    }
}
