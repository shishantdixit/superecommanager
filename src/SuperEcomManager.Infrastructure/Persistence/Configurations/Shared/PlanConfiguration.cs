using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Subscriptions;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Shared;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.MonthlyPrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.YearlyPrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .HasMaxLength(3);

        builder.HasMany(p => p.PlanFeatures)
            .WithOne(pf => pf.Plan)
            .HasForeignKey(pf => pf.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
