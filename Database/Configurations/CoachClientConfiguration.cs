using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class CoachClientConfiguration : IEntityTypeConfiguration<CoachClient>
{
    public void Configure(EntityTypeBuilder<CoachClient> builder)
    {
        builder.HasKey(bc => new { bc.ClientId, bc.CoachId });

        builder
            .HasOne(bc => bc.Client)
            .WithMany(b => b.Coaches)
            .HasForeignKey(bc => bc.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(bc => bc.Coach)
            .WithMany(c => c.Clients)
            .HasForeignKey(bc => bc.CoachId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}