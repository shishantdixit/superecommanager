using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Chat;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("chat_conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.MessageCount)
            .HasDefaultValue(0);

        builder.Property(c => c.TotalTokensUsed)
            .HasDefaultValue(0);

        builder.Property(c => c.Metadata)
            .HasColumnType("jsonb");

        // Navigation - Messages
        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.LastMessageAt);
        builder.HasIndex(c => new { c.UserId, c.Status });

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
    }
}
