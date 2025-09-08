using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class FunctionalMovementScreenConfiguration : IEntityTypeConfiguration<FunctionalMovementScreen>
{
    public void Configure(EntityTypeBuilder<FunctionalMovementScreen> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.Property(f => f.DeepSquat)
            .HasMaxLength(50);

        builder.Property(f => f.HurdleStep)
            .HasMaxLength(50);

        builder.Property(f => f.InLineLunge)
            .HasMaxLength(50);

        builder.Property(f => f.ActiveStraightLegRaise)
            .HasMaxLength(50);

        builder.Property(f => f.TrunkStabilityPushUp)
            .HasMaxLength(50);

        builder.Property(f => f.RotaryStability)
            .HasMaxLength(50);

        builder.Property(f => f.ShoulderMobility)
            .HasMaxLength(50);

        builder.ToTable("FunctionalMovementScreens", c 
            => c.IsTemporal());
    }
}