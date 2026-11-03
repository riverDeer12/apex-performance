using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class PriceListProductConfiguration : IEntityTypeConfiguration<PriceListProduct>
{
    public void Configure(EntityTypeBuilder<PriceListProduct> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(e => e.StripeProductId).HasMaxLength(100);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.Brand).HasMaxLength(100);
        builder.Property(e => e.UnitOfMeasure).HasMaxLength(20);
        builder.Property(e => e.Barcode).HasMaxLength(50);
        builder.Property(e => e.Category).HasMaxLength(100);

        builder.Property(e => e.NetQuantity).HasColumnType("decimal(10,3)");
        builder.Property(e => e.AnchorPrice).HasColumnType("decimal(10,2)");

        builder.HasIndex(e => e.StripeProductId);

        builder.ToTable("PriceListProducts", c => c.IsTemporal());
    }
}
