using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class AppointmentLocationConfiguration : IEntityTypeConfiguration<AppointmentLocation>
{
    public void Configure(EntityTypeBuilder<AppointmentLocation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);

        builder.Property(e => e.Address).HasMaxLength(300);

        // Up to 6 decimals is ~10 cm precision.
        builder.Property(e => e.Latitude).HasPrecision(9, 6);

        builder.Property(e => e.Longitude).HasPrecision(9, 6);

        builder.Property(e => e.GoogleMapsUrl).HasMaxLength(500);

        builder.ToTable("AppointmentLocations");

        builder.HasData(new AppointmentLocation
        {
            Id = AppointmentLocationIds.LegacyGymSkurinje,
            Name = "Legacy Gym Škurinje",
            // Plus Code 9C38+3H Rijeka (full code 8FQP9C38+3H).
            Address = "9C38+3H Rijeka",
            Latitude = 45.352688m,
            Longitude = 14.416438m,
            GoogleMapsUrl = "https://maps.app.goo.gl/bDCjBtfCE74w53xo6",
            CreatedAt = new DateTimeOffset(2026, 9, 29, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 9, 29, 0, 0, 0, TimeSpan.Zero)
        });
    }
}

public static class AppointmentLocationIds
{
    public static readonly Guid LegacyGymSkurinje = new("b3f1c2a4-5d6e-4f70-8a91-2c3d4e5f6a7b");
}
