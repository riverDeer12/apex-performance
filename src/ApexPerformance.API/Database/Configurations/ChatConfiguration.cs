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
