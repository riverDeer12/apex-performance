using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class TrainingLocationConfiguration : IEntityTypeConfiguration<TrainingLocation>
{
    public void Configure(EntityTypeBuilder<TrainingLocation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);

        builder.Property(e => e.Address).HasMaxLength(300);

        // Up to 6 decimals is ~10 cm precision.
        builder.Property(e => e.Latitude).HasPrecision(9, 6);

        builder.Property(e => e.Longitude).HasPrecision(9, 6);

        builder.Property(e => e.GoogleMapsUrl).HasMaxLength(500);

        builder.ToTable("TrainingLocations");
    }
}
