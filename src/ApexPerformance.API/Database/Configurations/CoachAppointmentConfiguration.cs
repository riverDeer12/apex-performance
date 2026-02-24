using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class CoachAppointmentConfiguration : IEntityTypeConfiguration<CoachAppointment>
{
    public void Configure(EntityTypeBuilder<CoachAppointment> builder)
    {
        builder.HasKey(bc => new { bc.AppointmentId, bc.CoachId });

        builder
            .HasOne(bc => bc.Appointment)
            .WithMany(b => b.Coaches)
            .HasForeignKey(bc => bc.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(bc => bc.Coach)
            .WithMany(c => c.Appointments)
            .HasForeignKey(bc => bc.CoachId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}