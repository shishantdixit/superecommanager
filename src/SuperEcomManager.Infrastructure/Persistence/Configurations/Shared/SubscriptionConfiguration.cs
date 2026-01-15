using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Subscriptions;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Shared;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.PriceAtSubscription)
            .HasPrecision(18, 2);

        builder.Property(s => s.Currency)
            .HasMaxLength(3);

        builder.Property(s => s.CancellationReason)
            .HasMaxLength(500);

        builder.HasIndex(s => new { s.TenantId, s.Status });

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
