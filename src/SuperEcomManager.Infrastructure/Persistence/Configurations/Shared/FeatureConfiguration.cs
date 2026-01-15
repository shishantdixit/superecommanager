using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Subscriptions;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Shared;

public class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.ToTable("features");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(f => f.Code)
            .IsUnique();

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Module)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Description)
            .HasMaxLength(500);

        builder.HasIndex(f => f.Module);
    }
}
