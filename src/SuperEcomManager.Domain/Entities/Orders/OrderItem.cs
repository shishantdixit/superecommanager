using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Domain.Entities.Orders;

/// <summary>
/// Represents a line item in an order.
/// </summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string? ExternalProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? VariantName { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero;
    public Money DiscountAmount { get; private set; } = Money.Zero;
    public Money TaxAmount { get; private set; } = Money.Zero;
    public Money TotalAmount { get; private set; } = Money.Zero;
    public decimal? Weight { get; private set; }
    public string? ImageUrl { get; private set; }

    // Reference to inventory
    public Guid? ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }

    public Order? Order { get; private set; }

    private OrderItem() { } // EF Core constructor

    public OrderItem(
        Guid orderId,
        string sku,
        string name,
        int quantity,
        Money unitPrice,
        string? externalProductId = null,
        string? variantName = null)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        Id = Guid.NewGuid();
        OrderId = orderId;
        Sku = sku;
        ExternalProductId = externalProductId;
        Name = name;
        VariantName = variantName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalAmount = unitPrice * quantity;
    }

    public void SetFinancials(Money discountAmount, Money taxAmount)
    {
        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        TotalAmount = (UnitPrice * Quantity) - DiscountAmount + TaxAmount;
    }

    public void LinkToProduct(Guid productId, Guid? variantId = null)
    {
        ProductId = productId;
        ProductVariantId = variantId;
    }

    public void SetAdditionalInfo(decimal? weight, string? imageUrl)
    {
        Weight = weight;
        ImageUrl = imageUrl;
    }
}
