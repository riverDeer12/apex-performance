using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(bc => new { bc.PermissionId, bc.RoleId });

        builder
            .HasOne(bc => bc.Permission)
            .WithMany(b => b.Roles)
            .HasForeignKey(bc => bc.PermissionId);

        builder
            .HasOne(bc => bc.Role)
            .WithMany(c => c.Permissions)
            .HasForeignKey(bc => bc.RoleId);
    }
}