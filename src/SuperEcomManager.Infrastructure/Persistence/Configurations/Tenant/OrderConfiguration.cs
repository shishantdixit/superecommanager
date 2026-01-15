using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Orders;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.ExternalOrderId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(o => new { o.ChannelId, o.ExternalOrderId })
            .IsUnique();

        builder.Property(o => o.ExternalOrderNumber)
            .HasMaxLength(100);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.FulfillmentStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.CustomerEmail)
            .HasMaxLength(255);

        builder.Property(o => o.CustomerPhone)
            .HasMaxLength(20);

        // Address as owned types stored as JSON
        builder.OwnsOne(o => o.ShippingAddress, address =>
        {
            address.ToJson("shipping_address");
        });

        builder.OwnsOne(o => o.BillingAddress, address =>
        {
            address.ToJson("billing_address");
        });

        // Money value objects
        builder.OwnsOne(o => o.Subtotal, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("subtotal")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("subtotal_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(o => o.DiscountAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("discount_amount")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("discount_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(o => o.TaxAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("tax_amount")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("tax_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(o => o.ShippingAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("shipping_amount")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("shipping_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(o => o.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("total_currency")
                .HasMaxLength(3);
        });

        builder.Property(o => o.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.CustomerNotes)
            .HasMaxLength(1000);

        builder.Property(o => o.InternalNotes)
            .HasMaxLength(1000);

        builder.Property(o => o.PlatformData)
            .HasColumnType("jsonb");

        // Navigation properties
        builder.HasOne(o => o.Channel)
            .WithMany()
            .HasForeignKey(o => o.ChannelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.OrderDate);
        builder.HasIndex(o => o.CreatedAt);
        builder.HasIndex(o => o.DeletedAt)
            .HasFilter("deleted_at IS NULL");

        // Ignore domain events - they're not persisted
        builder.Ignore(o => o.DomainEvents);
    }
}
