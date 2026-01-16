using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Shipments;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ShipmentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.ShipmentNumber)
            .IsUnique();

        builder.Property(s => s.AwbNumber)
            .HasMaxLength(100);

        builder.HasIndex(s => s.AwbNumber);

        builder.Property(s => s.CourierType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.CourierName)
            .HasMaxLength(100);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.LabelUrl)
            .HasMaxLength(1000);

        builder.Property(s => s.TrackingUrl)
            .HasMaxLength(1000);

        builder.Property(s => s.CourierResponse)
            .HasMaxLength(5000);

        // Configure Address value objects
        builder.OwnsOne(s => s.PickupAddress, address =>
        {
            address.Property(a => a.Name).HasColumnName("pickup_name").HasMaxLength(200);
            address.Property(a => a.Phone).HasColumnName("pickup_phone").HasMaxLength(20);
            address.Property(a => a.Line1).HasColumnName("pickup_line1").HasMaxLength(500);
            address.Property(a => a.Line2).HasColumnName("pickup_line2").HasMaxLength(500);
            address.Property(a => a.City).HasColumnName("pickup_city").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("pickup_state").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("pickup_postal_code").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("pickup_country").HasMaxLength(100);
        });

        builder.OwnsOne(s => s.DeliveryAddress, address =>
        {
            address.Property(a => a.Name).HasColumnName("delivery_name").HasMaxLength(200);
            address.Property(a => a.Phone).HasColumnName("delivery_phone").HasMaxLength(20);
            address.Property(a => a.Line1).HasColumnName("delivery_line1").HasMaxLength(500);
            address.Property(a => a.Line2).HasColumnName("delivery_line2").HasMaxLength(500);
            address.Property(a => a.City).HasColumnName("delivery_city").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("delivery_state").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("delivery_postal_code").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("delivery_country").HasMaxLength(100);
        });

        // Configure Dimensions value object
        builder.OwnsOne(s => s.Dimensions, dims =>
        {
            dims.Property(d => d.LengthCm).HasColumnName("length_cm").HasPrecision(10, 2);
            dims.Property(d => d.WidthCm).HasColumnName("width_cm").HasPrecision(10, 2);
            dims.Property(d => d.HeightCm).HasColumnName("height_cm").HasPrecision(10, 2);
            dims.Property(d => d.WeightKg).HasColumnName("weight_kg").HasPrecision(10, 3);
        });

        // Configure Money value objects
        builder.OwnsOne(s => s.ShippingCost, money =>
        {
            money.Property(m => m.Amount).HasColumnName("shipping_cost").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("shipping_cost_currency").HasMaxLength(3);
        });

        builder.OwnsOne(s => s.CODAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("cod_amount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("cod_currency").HasMaxLength(3);
        });

        builder.HasMany(s => s.Items)
            .WithOne(i => i.Shipment)
            .HasForeignKey(i => i.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.TrackingEvents)
            .WithOne(t => t.Shipment)
            .HasForeignKey(t => t.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.OrderId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CourierType);
    }
}

public class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("shipment_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Sku)
            .HasMaxLength(100);

        builder.Property(i => i.Name)
            .HasMaxLength(500);

        builder.HasOne(i => i.Shipment)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.ShipmentId);
    }
}

public class ShipmentTrackingConfiguration : IEntityTypeConfiguration<ShipmentTracking>
{
    public void Configure(EntityTypeBuilder<ShipmentTracking> builder)
    {
        builder.ToTable("shipment_trackings");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Location)
            .HasMaxLength(200);

        builder.Property(t => t.Remarks)
            .HasMaxLength(1000);

        builder.HasIndex(t => t.ShipmentId);
        builder.HasIndex(t => t.EventTime);
    }
}
