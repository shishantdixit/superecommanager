using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Domain.Entities.Inventory;

/// <summary>
/// Represents a product variant (size, color, etc.).
/// </summary>
public class ProductVariant : AuditableEntity
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Option1Name { get; private set; }
    public string? Option1Value { get; private set; }
    public string? Option2Name { get; private set; }
    public string? Option2Value { get; private set; }
    public Money? CostPrice { get; private set; }
    public Money? SellingPrice { get; private set; }
    public decimal? Weight { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }

    public Product? Product { get; private set; }

    private ProductVariant() { }

    public static ProductVariant Create(
        Guid productId,
        string sku,
        string name)
    {
        return new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = sku.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetOptions(string? option1Name, string? option1Value, string? option2Name = null, string? option2Value = null)
    {
        Option1Name = option1Name;
        Option1Value = option1Value;
        Option2Name = option2Name;
        Option2Value = option2Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPricing(Money? costPrice, Money? sellingPrice)
    {
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetWeight(decimal? weight)
    {
        Weight = weight;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImageUrl(string? imageUrl)
    {
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
