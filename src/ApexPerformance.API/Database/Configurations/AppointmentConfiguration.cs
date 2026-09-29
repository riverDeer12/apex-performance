using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder
            .HasOne(a => a.AppointmentType)
            .WithMany(b => b.Appointments)
            .HasForeignKey(b => b.AppointmentTypeId);
        
        builder
            .HasOne(a => a.AppointmentStatus)
            .WithMany(b => b.Appointments)
            .HasForeignKey(b => b.AppointmentStatusId);

        // Location is optional, deleting location
        // must not delete appointments held there.
        builder
            .HasOne(a => a.TrainingLocation)
            .WithMany(b => b.Appointments)
            .HasForeignKey(b => b.TrainingLocationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("Appointments", c => c.IsTemporal());
    }
}