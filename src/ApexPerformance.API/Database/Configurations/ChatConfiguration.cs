using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Coach)
            .WithMany()
            .HasForeignKey(e => e.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Messages)
            .WithOne(e => e.Conversation)
            .HasForeignKey(e => e.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // One conversation per coach and client.
        builder.HasIndex(e => new { e.CoachId, e.ClientId }).IsUnique();

        builder.ToTable("ChatConversations");
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.KeyId).HasMaxLength(16);

        builder.Property(e => e.AttachmentContentType).HasMaxLength(50);

        builder.HasIndex(e => new { e.ConversationId, e.CreatedAt });

        builder.ToTable("ChatMessages");
    }
}

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type).HasMaxLength(50);
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Body).HasMaxLength(1000);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The list of a user, newest first.
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });

        builder.ToTable("UserNotifications");
    }
}

public class ChatAttachmentConfiguration : IEntityTypeConfiguration<ChatAttachment>
{
    public void Configure(EntityTypeBuilder<ChatAttachment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ContentType).HasMaxLength(50);
        builder.Property(e => e.KeyId).HasMaxLength(16);

        builder.HasOne(e => e.Conversation)
            .WithMany()
            .HasForeignKey(e => e.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("ChatAttachments");
    }
}
