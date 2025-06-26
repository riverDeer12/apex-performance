using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class CoachAppointmentConfiguration : IEntityTypeConfiguration<CoachAppointment>
{
    public void Configure(EntityTypeBuilder<CoachAppointment> builder)
    {
        builder.HasKey(bc => new { bc.CoachId, bc.AppointmentId });

        builder
            .HasOne(bc => bc.Coach)
            .WithMany(b => b.Appointments)
            .HasForeignKey(bc => bc.CoachId);

        builder
            .HasOne(bc => bc.Appointment)
            .WithMany(c => c.Coaches)
            .HasForeignKey(bc => bc.AppointmentId);
    }
}