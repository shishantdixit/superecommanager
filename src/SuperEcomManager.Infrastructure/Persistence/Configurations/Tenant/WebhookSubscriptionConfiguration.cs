using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Webhooks;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("webhook_subscriptions");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(w => w.Secret)
            .HasMaxLength(500);

        builder.Property(w => w.IsActive)
            .HasDefaultValue(true);

        // Use jsonb instead of hstore for Headers dictionary
        // This avoids requiring the hstore PostgreSQL extension
        builder.Property(w => w.Headers)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        // Store Events as PostgreSQL integer array (enum values)
        // This allows proper EF Core translation for Contains queries
        builder.Property(w => w.Events)
            .HasColumnType("integer[]");

        builder.Property(w => w.MaxRetries)
            .HasDefaultValue(3);

        builder.Property(w => w.TimeoutSeconds)
            .HasDefaultValue(30);

        builder.HasMany(w => w.Deliveries)
            .WithOne(d => d.Subscription)
            .HasForeignKey(d => d.WebhookSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.IsActive);
    }
}
