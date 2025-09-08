using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class AppointmentRequestConfiguration : IEntityTypeConfiguration<AppointmentRequest>
{
    public void Configure(EntityTypeBuilder<AppointmentRequest> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.Property(e => e.Comment).HasMaxLength(200);
        
        builder.ToTable("AppointmentRequests");
    }
}