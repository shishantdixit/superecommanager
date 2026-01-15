using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Subscriptions;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Shared;

public class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("plan_features");

        // Composite primary key
        builder.HasKey(pf => new { pf.PlanId, pf.FeatureId });

        builder.HasOne(pf => pf.Plan)
            .WithMany(p => p.PlanFeatures)
            .HasForeignKey(pf => pf.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pf => pf.Feature)
            .WithMany()
            .HasForeignKey(pf => pf.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
