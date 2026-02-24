using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class AppointmentStatusConfiguration : IEntityTypeConfiguration<AppointmentStatus>
{
    public void Configure(EntityTypeBuilder<AppointmentStatus> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(200);
        builder.ToTable("AppointmentStatuses");
    }
}