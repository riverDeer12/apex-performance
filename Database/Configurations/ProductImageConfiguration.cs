using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(b => b.Name).HasMaxLength(200);
        
        builder.Property(b => b.Source).HasMaxLength(200);
        
        builder.ToTable("ProductImages", c => c.IsTemporal());
    }
}