using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Orders;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.ExternalProductId)
            .HasMaxLength(100);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.VariantName)
            .HasMaxLength(200);

        builder.Property(i => i.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(i => i.Weight)
            .HasPrecision(10, 3);

        // Money value objects
        builder.OwnsOne(i => i.UnitPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("unit_price")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("unit_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(i => i.DiscountAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("discount_amount")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("discount_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(i => i.TaxAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("tax_amount")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("tax_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(i => i.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("total_currency")
                .HasMaxLength(3);
        });

        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.Sku);
    }
}
