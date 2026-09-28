using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.IpAddress).HasMaxLength(64);

        builder.Property(e => e.UserAgent).HasMaxLength(512);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Last login per user is looked up by user and time.
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });

        builder.ToTable("UserSessions");
    }
}
