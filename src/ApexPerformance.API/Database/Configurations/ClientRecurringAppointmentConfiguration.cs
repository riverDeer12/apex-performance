using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class ClientRecurringAppointmentConfiguration : IEntityTypeConfiguration<ClientRecurringAppointment>
{
    public void Configure(EntityTypeBuilder<ClientRecurringAppointment> builder)
    {
        builder.HasKey(bc => new { bc.ClientId, bc.RecurringAppointmentId });

        builder
            .HasOne(bc => bc.Client)
            .WithMany(b => b.RecurringAppointments)
            .HasForeignKey(bc => bc.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(bc => bc.RecurringAppointment)
            .WithMany(c => c.Clients)
            .HasForeignKey(bc => bc.RecurringAppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}