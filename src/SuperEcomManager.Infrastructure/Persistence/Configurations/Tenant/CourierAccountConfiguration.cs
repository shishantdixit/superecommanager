using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Shipping;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class CourierAccountConfiguration : IEntityTypeConfiguration<CourierAccount>
{
    public void Configure(EntityTypeBuilder<CourierAccount> builder)
    {
        builder.ToTable("courier_accounts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.CourierType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.ApiKey)
            .HasMaxLength(500);

        builder.Property(c => c.ApiSecret)
            .HasMaxLength(500);

        builder.Property(c => c.AccessToken)
            .HasColumnType("text");

        builder.Property(c => c.AccountId)
            .HasMaxLength(100);

        builder.Property(c => c.ChannelId)
            .HasMaxLength(100);

        builder.Property(c => c.WebhookUrl)
            .HasMaxLength(500);

        builder.Property(c => c.WebhookSecret)
            .HasMaxLength(500);

        builder.Property(c => c.SettingsJson)
            .HasColumnType("jsonb");

        builder.Property(c => c.LastError)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(c => c.CourierType);
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.IsDefault);
        builder.HasIndex(c => new { c.CourierType, c.IsActive });
        builder.HasIndex(c => c.DeletedAt)
            .HasFilter("deleted_at IS NULL");
    }
}
