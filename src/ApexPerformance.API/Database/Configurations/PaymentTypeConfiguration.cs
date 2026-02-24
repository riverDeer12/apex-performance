using ApexPerformance.API.Database.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class PaymentTypeConfiguration : IEntityTypeConfiguration<PaymentType>
{
    public void Configure(EntityTypeBuilder<PaymentType> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name).HasMaxLength(200);
        
        builder.Property(e => e.Description).HasMaxLength(200);
        
        builder.ToTable("PaymentTypes");
    }
}