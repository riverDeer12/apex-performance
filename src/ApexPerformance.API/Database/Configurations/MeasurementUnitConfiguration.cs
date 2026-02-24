using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.Property(e => e.Name).HasMaxLength(200);
        
        builder.Property(e => e.Symbol).HasMaxLength(20);
        
        builder.ToTable("MeasurementUnits", c => c.IsTemporal());
    }
}