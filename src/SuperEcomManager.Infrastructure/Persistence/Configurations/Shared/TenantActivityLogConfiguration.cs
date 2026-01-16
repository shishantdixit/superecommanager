using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Platform;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Shared;

public class TenantActivityLogConfiguration : IEntityTypeConfiguration<TenantActivityLog>
{
    public void Configure(EntityTypeBuilder<TenantActivityLog> builder)
    {
        builder.ToTable("tenant_activity_logs");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Details)
            .HasMaxLength(2000);

        builder.Property(t => t.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.PerformedBy);
        builder.HasIndex(t => t.PerformedAt);
        builder.HasIndex(t => new { t.TenantId, t.PerformedAt });
    }
}
