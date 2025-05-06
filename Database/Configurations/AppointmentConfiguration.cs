using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder
            .HasOne(a => a.AppointmentType)
            .WithMany(b => b.Appointments)
            .HasForeignKey(b => b.AppointmentTypeId);

        builder.ToTable("Appointments", c => c.IsTemporal());
    }
}