using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Inventory;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Category)
            .HasMaxLength(200);

        builder.Property(p => p.Brand)
            .HasMaxLength(200);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.HsnCode)
            .HasMaxLength(20);

        builder.Property(p => p.Weight)
            .HasPrecision(10, 3);

        builder.Property(p => p.TaxRate)
            .HasPrecision(5, 2);

        // Configure Money value objects
        builder.OwnsOne(p => p.CostPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("cost_price")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("cost_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(p => p.SellingPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("selling_price")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("selling_currency")
                .HasMaxLength(3);
        });

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.Brand);
        builder.HasIndex(p => p.IsActive);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(v => v.Sku)
            .IsUnique();

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.Option1Name)
            .HasMaxLength(100);

        builder.Property(v => v.Option1Value)
            .HasMaxLength(200);

        builder.Property(v => v.Option2Name)
            .HasMaxLength(100);

        builder.Property(v => v.Option2Value)
            .HasMaxLength(200);

        builder.Property(v => v.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(v => v.Weight)
            .HasPrecision(10, 3);

        // Configure optional Money value objects
        builder.OwnsOne(v => v.CostPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("cost_price")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("cost_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(v => v.SellingPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("selling_price")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("selling_currency")
                .HasMaxLength(3);
        });

        builder.HasIndex(v => v.ProductId);
    }
}
