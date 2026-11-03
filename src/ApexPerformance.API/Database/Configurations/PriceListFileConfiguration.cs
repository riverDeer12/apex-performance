using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class PriceListFileConfiguration : IEntityTypeConfiguration<PriceListFile>
{
    public void Configure(EntityTypeBuilder<PriceListFile> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(e => e.FileName).HasMaxLength(200);
        builder.Property(e => e.Reason).HasMaxLength(50);

        builder.HasIndex(e => e.FileName);
        builder.HasIndex(e => e.PublishedAt);

        builder.ToTable("PriceListFiles");
    }
}
