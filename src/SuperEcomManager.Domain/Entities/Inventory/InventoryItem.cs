using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Inventory;

/// <summary>
/// Represents inventory/stock level for a product variant.
/// </summary>
public class InventoryItem : AuditableEntity
{
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }
    public int QuantityAvailable => QuantityOnHand - QuantityReserved;
    public int ReorderPoint { get; private set; }
    public int ReorderQuantity { get; private set; }
    public string? Location { get; private set; }

    public Product? Product { get; private set; }
    public ProductVariant? ProductVariant { get; private set; }

    private InventoryItem() { }

    public static InventoryItem Create(Guid productId, string sku, Guid? variantId = null)
    {
        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductVariantId = variantId,
            Sku = sku,
            QuantityOnHand = 0,
            QuantityReserved = 0,
            ReorderPoint = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        QuantityOnHand += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        if (quantity > QuantityAvailable)
            throw new InvalidOperationException("Insufficient stock available");
        QuantityOnHand -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reserve(int quantity)
    {
        if (quantity > QuantityAvailable)
            throw new InvalidOperationException("Insufficient stock to reserve");
        QuantityReserved += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseReservation(int quantity)
    {
        QuantityReserved = Math.Max(0, QuantityReserved - quantity);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLowStock() => QuantityOnHand <= ReorderPoint;
}
