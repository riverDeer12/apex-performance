using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action).HasMaxLength(16);

        builder.Property(e => e.EntityType).HasMaxLength(64);

        builder.Property(e => e.EntityId).HasMaxLength(64);

        builder.Property(e => e.EntityName).HasMaxLength(200);

        builder.HasIndex(e => e.Timestamp);

        builder.HasIndex(e => new { e.EntityType, e.EntityId });

        builder.HasIndex(e => new { e.UserId, e.Timestamp });

        builder.ToTable("AuditLogs");
    }
}
