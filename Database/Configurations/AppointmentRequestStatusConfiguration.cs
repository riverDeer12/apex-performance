using ApexPerformance.API.Database.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class AppointmentRequestStatusConfiguration : IEntityTypeConfiguration<AppointmentRequestStatus>
{
    public void Configure(EntityTypeBuilder<AppointmentRequestStatus> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(200);
        builder.ToTable("AppointmentRequestStatuses");

    }
}