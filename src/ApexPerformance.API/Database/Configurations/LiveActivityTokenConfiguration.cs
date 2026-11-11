using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class LiveActivityTokenConfiguration : IEntityTypeConfiguration<LiveActivityToken>
{
    public void Configure(EntityTypeBuilder<LiveActivityToken> builder)
    {
        // Same filter as the device it belongs to.
        builder.HasQueryFilter(x => !x.DeviceToken!.IsDeleted);

        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);

        builder.Property(x => x.Token).HasMaxLength(400);

        builder.HasOne(x => x.DeviceToken)
            .WithMany()
            .HasForeignKey(x => x.DeviceTokenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.DeviceTokenId, x.Kind, x.TrainingId });

        builder.HasIndex(x => x.TrainingId);

        builder.ToTable("LiveActivityTokens");
    }
}
