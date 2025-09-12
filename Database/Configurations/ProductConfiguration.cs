using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name).HasMaxLength(200);
        
        builder.Property(e => e.Description).HasMaxLength(200);
        
        builder.Property(b => b.Price).HasColumnType("decimal(5,2)");
        
        builder.ToTable("Products", c => c.IsTemporal());
    }
}