using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(bc => new { bc.UserId, bc.RoleId });

        builder
            .HasOne(bc => bc.User)
            .WithMany(b => b.Roles)
            .HasForeignKey(bc => bc.UserId);

        builder
            .HasOne(bc => bc.Role)
            .WithMany(c => c.Users)
            .HasForeignKey(bc => bc.RoleId);
    }
}