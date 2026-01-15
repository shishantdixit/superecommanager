using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Inventory;

/// <summary>
/// Query to get inventory statistics.
/// </summary>
[RequirePermission("inventory.view")]
[RequireFeature("inventory_management")]
public record GetInventoryStatsQuery : IRequest<Result<InventoryStatsDto>>, ITenantRequest
{
}

public class GetInventoryStatsQueryHandler : IRequestHandler<GetInventoryStatsQuery, Result<InventoryStatsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetInventoryStatsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<InventoryStatsDto>> Handle(
        GetInventoryStatsQuery request,
        CancellationToken cancellationToken)
    {
        // Get products
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var totalProducts = products.Count;
        var activeProducts = products.Count(p => p.IsActive);

        // Get variants count
        var totalVariants = await _dbContext.ProductVariants
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // Get inventory items
        var inventoryItems = await _dbContext.Inventory
            .Include(i => i.Product)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalStockOnHand = inventoryItems.Sum(i => i.QuantityOnHand);
        var totalStockReserved = inventoryItems.Sum(i => i.QuantityReserved);

        // Low stock and out of stock
        var lowStockItems = inventoryItems.Where(i => i.IsLowStock()).ToList();
        var outOfStockItems = inventoryItems.Where(i => i.QuantityOnHand == 0).ToList();

        // Calculate inventory value (cost price * quantity)
        var productDict = products.ToDictionary(p => p.Id, p => p);
        decimal totalInventoryValue = 0;
        string currency = "INR";

        foreach (var item in inventoryItems)
        {
            if (productDict.TryGetValue(item.ProductId, out var product))
            {
                totalInventoryValue += product.CostPrice.Amount * item.QuantityOnHand;
                currency = product.CostPrice.Currency;
            }
        }

        // Stock by category
        var stockByCategory = inventoryItems
            .Where(i => i.Product?.Category != null)
            .GroupBy(i => i.Product!.Category!)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.QuantityOnHand));

        // Build low stock items list
        var lowStockItemDtos = lowStockItems
            .Take(20)
            .Select(i =>
            {
                productDict.TryGetValue(i.ProductId, out var product);
                return new LowStockItemDto
                {
                    ProductId = i.ProductId,
                    VariantId = i.ProductVariantId,
                    Sku = i.Sku,
                    ProductName = product?.Name ?? "Unknown",
                    QuantityOnHand = i.QuantityOnHand,
                    ReorderPoint = i.ReorderPoint,
                    ReorderQuantity = i.ReorderQuantity
                };
            })
            .ToList();

        var stats = new InventoryStatsDto
        {
            TotalProducts = totalProducts,
            TotalActiveProducts = activeProducts,
            TotalVariants = totalVariants,
            TotalStockOnHand = totalStockOnHand,
            TotalStockReserved = totalStockReserved,
            LowStockProducts = lowStockItems.Select(i => i.ProductId).Distinct().Count(),
            OutOfStockProducts = outOfStockItems.Select(i => i.ProductId).Distinct().Count(),
            TotalInventoryValue = totalInventoryValue,
            Currency = currency,
            StockByCategory = stockByCategory,
            LowStockItems = lowStockItemDtos
        };

        return Result<InventoryStatsDto>.Success(stats);
    }
}
