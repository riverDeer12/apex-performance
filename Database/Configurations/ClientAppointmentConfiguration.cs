using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class ClientAppointmentConfiguration : IEntityTypeConfiguration<ClientAppointment>
{
    public void Configure(EntityTypeBuilder<ClientAppointment> builder)
    {
        builder.HasKey(bc => new { bc.ClientId, bc.AppointmentId });

        builder
            .HasOne(bc => bc.Client)
            .WithMany(b => b.Appointments)
            .HasForeignKey(bc => bc.ClientId);

        builder
            .HasOne(bc => bc.Appointment)
            .WithMany(c => c.Clients)
            .HasForeignKey(bc => bc.AppointmentId);
    }
}