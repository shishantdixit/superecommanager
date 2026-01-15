using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Identity;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.HasIndex(r => r.Name).IsUnique();
    }
}
