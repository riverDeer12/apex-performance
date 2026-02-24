using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class PaymentProductConfiguration : IEntityTypeConfiguration<PaymentProduct>
{
    public void Configure(EntityTypeBuilder<PaymentProduct> builder)
    {
        builder.HasKey(bc => new { bc.ProductId, bc.PaymentId });

        builder
            .HasOne(bc => bc.Product)
            .WithMany(b => b.Payments)
            .HasForeignKey(bc => bc.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(bc => bc.Payment)
            .WithMany(c => c.Products)
            .HasForeignKey(bc => bc.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}