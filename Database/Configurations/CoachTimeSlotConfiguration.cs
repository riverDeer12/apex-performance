using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class CoachTimeSlotConfiguration : IEntityTypeConfiguration<CoachTimeSlot>
{
    public void Configure(EntityTypeBuilder<CoachTimeSlot> builder)
    {
        builder.HasKey(bc => new { bc.CoachId, bc.TimeSlotId });

        builder
            .HasOne(bc => bc.Coach)
            .WithMany(b => b.TimeSlots)
            .HasForeignKey(bc => bc.CoachId);

        builder
            .HasOne(bc => bc.TimeSlot)
            .WithMany(c => c.Coaches)
            .HasForeignKey(bc => bc.TimeSlotId);
    }
}