using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Chat;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(m => m.Sequence)
            .IsRequired();

        builder.Property(m => m.ToolCallId)
            .HasMaxLength(100);

        builder.Property(m => m.ToolName)
            .HasMaxLength(100);

        builder.Property(m => m.ToolCalls)
            .HasColumnType("jsonb");

        builder.Property(m => m.Metadata)
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => new { m.ConversationId, m.Sequence });
        builder.HasIndex(m => m.CreatedAt);

        // Ignore domain events
        builder.Ignore(m => m.DomainEvents);
    }
}
