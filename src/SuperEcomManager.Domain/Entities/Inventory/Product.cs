using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Domain.Entities.Inventory;

/// <summary>
/// Represents a product in the inventory.
/// </summary>
public class Product : AuditableEntity, ISoftDeletable
{
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string? Brand { get; private set; }
    public Money CostPrice { get; private set; } = Money.Zero;
    public Money SellingPrice { get; private set; } = Money.Zero;
    public decimal? Weight { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }
    public string? HsnCode { get; private set; }
    public decimal? TaxRate { get; private set; }

    // Sync tracking fields
    public SyncStatus SyncStatus { get; private set; } = SyncStatus.Synced;
    public DateTime? LastSyncedAt { get; private set; }
    public string? ChannelProductId { get; private set; }
    public Money? ChannelSellingPrice { get; private set; }

    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private readonly List<ProductVariant> _variants = new();
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    private Product() { }

    public static Product Create(
        string sku,
        string name,
        Money costPrice,
        Money sellingPrice)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            CostPrice = costPrice,
            SellingPrice = sellingPrice,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, string? category, string? brand)
    {
        Name = name;
        Description = description;
        Category = category;
        Brand = brand;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePricing(Money costPrice, Money sellingPrice)
    {
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImageUrl(string? imageUrl)
    {
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetWeight(decimal? weight)
    {
        Weight = weight;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTaxInfo(string? hsnCode, decimal? taxRate)
    {
        HsnCode = hsnCode;
        TaxRate = taxRate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSyncStatus(SyncStatus status)
    {
        SyncStatus = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSynced(string? channelProductId = null)
    {
        SyncStatus = SyncStatus.Synced;
        LastSyncedAt = DateTime.UtcNow;
        ChannelSellingPrice = SellingPrice;
        if (channelProductId != null)
            ChannelProductId = channelProductId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsLocalOnly()
    {
        SyncStatus = SyncStatus.LocalOnly;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPendingSync()
    {
        SyncStatus = SyncStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetChannelProductId(string channelProductId)
    {
        ChannelProductId = channelProductId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DetectConflict(Money channelPrice)
    {
        if (ChannelSellingPrice != null && !ChannelSellingPrice.Equals(channelPrice))
        {
            SyncStatus = SyncStatus.Conflict;
            ChannelSellingPrice = channelPrice;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddVariant(ProductVariant variant) => _variants.Add(variant);
}
