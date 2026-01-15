using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Inventory;

/// <summary>
/// Query to get a single product by ID with full details.
/// </summary>
[RequirePermission("inventory.view")]
[RequireFeature("inventory_management")]
public record GetProductByIdQuery : IRequest<Result<ProductDetailDto>>, ITenantRequest
{
    public Guid ProductId { get; init; }
}

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetProductByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ProductDetailDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .Include(p => p.Variants)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.DeletedAt == null, cancellationToken);

        if (product == null)
        {
            return Result<ProductDetailDto>.Failure("Product not found");
        }

        // Get inventory data
        var inventoryItems = await _dbContext.Inventory
            .AsNoTracking()
            .Where(i => i.ProductId == product.Id)
            .ToListAsync(cancellationToken);

        // Build variant DTOs with inventory
        var variantDtos = product.Variants.Select(v =>
        {
            var variantInventory = inventoryItems.FirstOrDefault(i => i.ProductVariantId == v.Id);
            return new ProductVariantDto
            {
                Id = v.Id,
                Sku = v.Sku,
                Name = v.Name,
                Option1Name = v.Option1Name,
                Option1Value = v.Option1Value,
                Option2Name = v.Option2Name,
                Option2Value = v.Option2Value,
                CostPrice = v.CostPrice?.Amount,
                SellingPrice = v.SellingPrice?.Amount,
                Weight = v.Weight,
                ImageUrl = v.ImageUrl,
                IsActive = v.IsActive,
                QuantityOnHand = variantInventory?.QuantityOnHand ?? 0,
                QuantityAvailable = variantInventory?.QuantityAvailable ?? 0
            };
        }).ToList();

        // Build inventory summary
        var inventorySummary = new InventorySummaryDto
        {
            TotalOnHand = inventoryItems.Sum(i => i.QuantityOnHand),
            TotalReserved = inventoryItems.Sum(i => i.QuantityReserved),
            TotalAvailable = inventoryItems.Sum(i => i.QuantityAvailable),
            IsLowStock = inventoryItems.Any(i => i.IsLowStock()),
            Items = inventoryItems.Select(i =>
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == i.ProductVariantId);
                return new InventoryItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    Sku = i.Sku,
                    VariantName = variant?.Name,
                    QuantityOnHand = i.QuantityOnHand,
                    QuantityReserved = i.QuantityReserved,
                    QuantityAvailable = i.QuantityAvailable,
                    ReorderPoint = i.ReorderPoint,
                    ReorderQuantity = i.ReorderQuantity,
                    Location = i.Location,
                    IsLowStock = i.IsLowStock()
                };
            }).ToList()
        };

        var dto = new ProductDetailDto
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            Category = product.Category,
            Brand = product.Brand,
            CostPrice = product.CostPrice.Amount,
            SellingPrice = product.SellingPrice.Amount,
            Currency = product.SellingPrice.Currency,
            Weight = product.Weight,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            HsnCode = product.HsnCode,
            TaxRate = product.TaxRate,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Variants = variantDtos,
            InventorySummary = inventorySummary
        };

        return Result<ProductDetailDto>.Success(dto);
    }
}
