using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Platform;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Shared;

public class PlatformAdminConfiguration : IEntityTypeConfiguration<PlatformAdmin>
{
    public void Configure(EntityTypeBuilder<PlatformAdmin> builder)
    {
        builder.ToTable("platform_admins");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(p => p.Email)
            .IsUnique();

        builder.Property(p => p.FirstName)
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .HasMaxLength(100);

        builder.Property(p => p.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.RefreshToken)
            .HasMaxLength(500);

        builder.Property(p => p.LastLoginIpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.HasIndex(p => p.RefreshToken);

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
