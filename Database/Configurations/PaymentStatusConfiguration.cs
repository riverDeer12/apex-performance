using ApexPerformance.API.Database.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class PaymentStatusConfiguration : IEntityTypeConfiguration<PaymentStatus>
{
    public void Configure(EntityTypeBuilder<PaymentStatus> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name).HasMaxLength(200);
        
        builder.Property(e => e.Description).HasMaxLength(200);
        
        builder.ToTable("PaymentStatuses");
    }
}