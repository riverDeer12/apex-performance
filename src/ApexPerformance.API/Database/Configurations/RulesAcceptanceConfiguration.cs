using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class RulesAcceptanceConfiguration : IEntityTypeConfiguration<RulesAcceptance>
{
    public void Configure(EntityTypeBuilder<RulesAcceptance> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Document).HasMaxLength(50);
        builder.Property(e => e.Version).HasMaxLength(20);
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasMaxLength(300);
        builder.Property(e => e.GuardianName).HasMaxLength(200);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ClientId, e.Document, e.AcceptedAt });

        builder.ToTable("RulesAcceptances");
    }
}
