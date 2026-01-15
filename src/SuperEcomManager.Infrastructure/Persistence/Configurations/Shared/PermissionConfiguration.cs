using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Identity;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Shared;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Module)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.Module);
    }
}
