using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class UserProfilePictureConfiguration : IEntityTypeConfiguration<UserProfilePicture>
{
    public void Configure(EntityTypeBuilder<UserProfilePicture> builder)
    {
        // One picture per user, so user id is the key.
        builder.HasKey(e => e.UserId);

        builder.Property(e => e.ContentType).HasMaxLength(50);

        builder.HasOne(e => e.User)
            .WithOne()
            .HasForeignKey<UserProfilePicture>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("UserProfilePictures");
    }
}
