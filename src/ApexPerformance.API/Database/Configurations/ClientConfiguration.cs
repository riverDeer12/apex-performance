using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.Property(e => e.FirstName).HasMaxLength(200);
        
        builder.Property(e => e.LastName).HasMaxLength(200);
        
        builder.Property(e => e.Email).HasMaxLength(200);
        
        builder.Property(e => e.Phone).HasMaxLength(200);

        builder.Property(e => e.Plan).HasMaxLength(50)
            .HasDefaultValue(Constants.ClientPlans.PrivateCoaching);
        
        builder.ToTable("Clients", c => c.IsTemporal());
    }
}