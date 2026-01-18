using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Inventory;

/// <summary>
/// Lightweight DTO for product list view.
/// </summary>
public record ProductListDto
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public string Currency { get; init; } = "INR";
    public bool IsActive { get; init; }
    public string? ImageUrl { get; init; }
    public int TotalStock { get; init; }
    public int VariantCount { get; init; }
    public DateTime CreatedAt { get; init; }

    // Sync tracking
    public SyncStatus SyncStatus { get; init; } = SyncStatus.Synced;
    public decimal? ChannelSellingPrice { get; init; }
    public DateTime? LastSyncedAt { get; init; }
}

/// <summary>
/// Full DTO for product details.
/// </summary>
public record ProductDetailDto
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public string Currency { get; init; } = "INR";
    public decimal? Weight { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
    public string? HsnCode { get; init; }
    public decimal? TaxRate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Sync tracking
    public SyncStatus SyncStatus { get; init; } = SyncStatus.Synced;
    public DateTime? LastSyncedAt { get; init; }
    public string? ChannelProductId { get; init; }
    public decimal? ChannelSellingPrice { get; init; }
    public string? ChannelSellingCurrency { get; init; }

    // Variants
    public List<ProductVariantDto> Variants { get; init; } = new();

    // Inventory
    public InventorySummaryDto? InventorySummary { get; init; }
}

/// <summary>
/// DTO for product variant.
/// </summary>
public record ProductVariantDto
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Option1Name { get; init; }
    public string? Option1Value { get; init; }
    public string? Option2Name { get; init; }
    public string? Option2Value { get; init; }
    public decimal? CostPrice { get; init; }
    public decimal? SellingPrice { get; init; }
    public decimal? Weight { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
    public int QuantityOnHand { get; init; }
    public int QuantityAvailable { get; init; }
}

/// <summary>
/// Inventory summary for a product.
/// </summary>
public record InventorySummaryDto
{
    public int TotalOnHand { get; init; }
    public int TotalReserved { get; init; }
    public int TotalAvailable { get; init; }
    public bool IsLowStock { get; init; }
    public List<InventoryItemDto> Items { get; init; } = new();
}

/// <summary>
/// DTO for inventory item.
/// </summary>
public record InventoryItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public int QuantityOnHand { get; init; }
    public int QuantityReserved { get; init; }
    public int QuantityAvailable { get; init; }
    public int ReorderPoint { get; init; }
    public int ReorderQuantity { get; init; }
    public string? Location { get; init; }
    public bool IsLowStock { get; init; }
}

/// <summary>
/// DTO for stock movement.
/// </summary>
public record StockMovementDto
{
    public Guid Id { get; init; }
    public Guid InventoryId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public MovementType MovementType { get; init; }
    public int Quantity { get; init; }
    public int QuantityBefore { get; init; }
    public int QuantityAfter { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
    public string? Notes { get; init; }
    public string? PerformedByUserName { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Filter parameters for products query.
/// </summary>
public record ProductFilterDto
{
    public string? SearchTerm { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsLowStock { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public SyncStatus? SyncStatus { get; init; }
}

/// <summary>
/// Sort options for products.
/// </summary>
public enum ProductSortBy
{
    Name,
    Sku,
    Price,
    Stock,
    CreatedAt
}

/// <summary>
/// Filter for stock movements.
/// </summary>
public record StockMovementFilterDto
{
    public Guid? ProductId { get; init; }
    public Guid? InventoryItemId { get; init; }
    public string? Sku { get; init; }
    public MovementType? MovementType { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

/// <summary>
/// Inventory statistics DTO.
/// </summary>
public record InventoryStatsDto
{
    public int TotalProducts { get; init; }
    public int TotalActiveProducts { get; init; }
    public int TotalVariants { get; init; }
    public int TotalStockOnHand { get; init; }
    public int TotalStockReserved { get; init; }
    public int LowStockProducts { get; init; }
    public int OutOfStockProducts { get; init; }
    public decimal TotalInventoryValue { get; init; }
    public string Currency { get; init; } = "INR";
    public Dictionary<string, int> StockByCategory { get; init; } = new();
    public List<LowStockItemDto> LowStockItems { get; init; } = new();
}

/// <summary>
/// DTO for low stock item alert.
/// </summary>
public record LowStockItemDto
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public int QuantityOnHand { get; init; }
    public int ReorderPoint { get; init; }
    public int ReorderQuantity { get; init; }
}

/// <summary>
/// DTO for creating a product.
/// </summary>
public record CreateProductDto
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public string Currency { get; init; } = "INR";
    public decimal? Weight { get; init; }
    public string? ImageUrl { get; init; }
    public string? HsnCode { get; init; }
    public decimal? TaxRate { get; init; }
    public int InitialStock { get; init; }
    public List<CreateVariantDto>? Variants { get; init; }
}

/// <summary>
/// DTO for creating a variant.
/// </summary>
public record CreateVariantDto
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Option1Name { get; init; }
    public string? Option1Value { get; init; }
    public string? Option2Name { get; init; }
    public string? Option2Value { get; init; }
    public decimal? CostPrice { get; init; }
    public decimal? SellingPrice { get; init; }
    public decimal? Weight { get; init; }
    public string? ImageUrl { get; init; }
    public int InitialStock { get; init; }
}

/// <summary>
/// DTO for stock adjustment.
/// </summary>
public record StockAdjustmentDto
{
    public Guid InventoryItemId { get; init; }
    public MovementType AdjustmentType { get; init; }
    public int Quantity { get; init; }
    public string? Notes { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
}
