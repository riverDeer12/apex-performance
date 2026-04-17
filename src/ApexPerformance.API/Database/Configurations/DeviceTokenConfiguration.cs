using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.Property(e => e.Token).HasMaxLength(200);
        
        builder.Property(e => e.Platform).HasMaxLength(200);
        
        builder.ToTable("DeviceTokens");
    }
}