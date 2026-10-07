using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class ClientGoalConfiguration : IEntityTypeConfiguration<ClientGoal>
{
    public void Configure(EntityTypeBuilder<ClientGoal> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(e => e.Goal).HasMaxLength(1000);
        builder.Property(e => e.CurrentBlock).HasMaxLength(1000);
        builder.Property(e => e.Focus).HasMaxLength(1000);
        builder.Property(e => e.NextAssessment).HasMaxLength(500);

        // One goal and plan per client, it is updated in place.
        builder.HasIndex(e => e.ClientId).IsUnique();

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("ClientGoals", c => c.IsTemporal());
    }
}
