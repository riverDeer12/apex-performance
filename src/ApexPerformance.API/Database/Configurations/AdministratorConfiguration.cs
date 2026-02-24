using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class AdministratorConfiguration : IEntityTypeConfiguration<Administrator>
{
    public void Configure(EntityTypeBuilder<Administrator> builder)
    {
        builder.Property(e => e.FirstName).HasMaxLength(200);
        builder.Property(e => e.LastName).HasMaxLength(200);
        builder.ToTable("Administrators", c => c.IsTemporal());
    }
}