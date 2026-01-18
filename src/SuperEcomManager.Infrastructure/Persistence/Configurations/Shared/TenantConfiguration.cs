using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Shared;

public class TenantConfiguration : IEntityTypeConfiguration<Domain.Entities.Tenants.Tenant>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Tenants.Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.Property(t => t.SchemaName)
            .IsRequired()
            .HasMaxLength(63); // PostgreSQL schema name limit

        builder.HasIndex(t => t.SchemaName)
            .IsUnique();

        builder.Property(t => t.CompanyName)
            .HasMaxLength(200);

        builder.Property(t => t.LogoUrl)
            .HasMaxLength(500);

        builder.Property(t => t.Website)
            .HasMaxLength(255);

        builder.Property(t => t.ContactEmail)
            .HasMaxLength(255);

        builder.Property(t => t.ContactPhone)
            .HasMaxLength(20);

        builder.Property(t => t.Address)
            .HasMaxLength(500);

        builder.Property(t => t.GstNumber)
            .HasMaxLength(20);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Soft delete index for performance
        builder.HasIndex(t => t.DeletedAt)
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}
