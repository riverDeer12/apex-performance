using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class RecurringAppointmentConfiguration : IEntityTypeConfiguration<RecurringAppointment>
{
    public void Configure(EntityTypeBuilder<RecurringAppointment> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder
            .HasOne(a => a.Client)
            .WithMany(b => b.RecurringAppointments)
            .HasForeignKey(b => b.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder
            .HasOne(a => a.Coach)
            .WithMany(b => b.RecurringAppointments)
            .HasForeignKey(b => b.CoachId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder
            .HasOne(a => a.TimeSlot)
            .WithMany(b => b.RecurringAppointments)
            .HasForeignKey(b => b.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("RecurringAppointments", c => c.IsTemporal());
    }
}