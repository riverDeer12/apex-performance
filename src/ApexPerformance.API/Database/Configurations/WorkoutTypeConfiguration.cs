using ApexPerformance.API.Database.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class WorkoutTypeConfiguration : IEntityTypeConfiguration<WorkoutType>
{
    public void Configure(EntityTypeBuilder<WorkoutType> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.ToTable("WorkoutTypes");
    }
}