using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Channels;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class SalesChannelConfiguration : IEntityTypeConfiguration<SalesChannel>
{
    public void Configure(EntityTypeBuilder<SalesChannel> builder)
    {
        builder.ToTable("sales_channels");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.StoreUrl)
            .HasMaxLength(500);

        builder.Property(c => c.StoreName)
            .HasMaxLength(200);

        builder.Property(c => c.ExternalShopId)
            .HasMaxLength(100);

        builder.Property(c => c.CredentialsEncrypted)
            .HasColumnType("text");

        builder.Property(c => c.WebhookSecret)
            .HasMaxLength(500);

        builder.Property(c => c.LastSyncStatus)
            .HasMaxLength(200);

        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.DeletedAt)
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}
