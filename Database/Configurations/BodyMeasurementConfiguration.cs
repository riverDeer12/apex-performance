using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class BodyMeasurementConfiguration : IEntityTypeConfiguration<BodyMeasurement>
{
    public void Configure(EntityTypeBuilder<BodyMeasurement> builder)
    {
        builder.Property(b => b.Height).HasColumnType("decimal(5,2)");
        builder.Property(b => b.Weight).HasColumnType("decimal(5,2)");
        builder.Property(b => b.Shoulders).HasColumnType("decimal(5,2)");
        builder.Property(b => b.Chest).HasColumnType("decimal(5,2)");
        builder.Property(b => b.UpperArm).HasColumnType("decimal(5,2)");
        builder.Property(b => b.Waist).HasColumnType("decimal(5,2)");
        builder.Property(b => b.Thigh).HasColumnType("decimal(5,2)");
        builder.Property(b => b.Calves).HasColumnType("decimal(5,2)");
        builder.Property(b => b.Glutes).HasColumnType("decimal(5,2)");
        
        builder.ToTable("BodyMeasurements", c => c.IsTemporal());
    }
}