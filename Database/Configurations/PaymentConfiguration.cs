using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(b => b.Price).HasColumnType("decimal(5,2)");
        
        builder.Property(e => e.CustomerFirstName).HasMaxLength(200);
        
        builder.Property(e => e.CustomerLastName).HasMaxLength(200);
        
        builder.Property(e => e.CustomerPhone).HasMaxLength(200);
        
        builder.Property(e => e.CustomerEmail).HasMaxLength(200);
        
        builder.Property(e => e.CustomerAddress).HasMaxLength(200);
        
        builder
            .HasOne(a => a.PaymentType)
            .WithMany(b => b.Payments)
            .HasForeignKey(b => b.PaymentTypeId);
        
        builder
            .HasOne(a => a.PaymentStatus)
            .WithMany(b => b.Payments)
            .HasForeignKey(b => b.PaymentStatusId);
        
        builder.ToTable("Payments", c => c.IsTemporal());
    }
}