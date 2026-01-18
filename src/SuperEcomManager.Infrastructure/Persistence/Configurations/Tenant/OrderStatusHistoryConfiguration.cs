using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Orders;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        // Table name matches existing migration
        builder.ToTable("OrderStatusHistory");

        builder.HasKey(h => h.Id);

        // Status stored as integer to match existing migration
        builder.Property(h => h.Status)
            .IsRequired();

        builder.Property(h => h.Reason)
            .HasMaxLength(500);

        builder.Property(h => h.ChangedAt)
            .IsRequired();

        builder.HasIndex(h => h.OrderId);
        builder.HasIndex(h => h.ChangedAt);
    }
}
