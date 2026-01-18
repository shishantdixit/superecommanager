using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Application.Features.Inventory;

/// <summary>
/// Sync mode for product updates.
/// </summary>
public enum ProductSyncMode
{
    /// <summary>Save locally only, never sync to channel</summary>
    LocalOnly = 0,

    /// <summary>Save locally and queue for later sync</summary>
    PendingSync = 1,

    /// <summary>Save locally and sync to channel immediately</summary>
    SyncImmediately = 2
}

/// <summary>
/// Command to update a product.
/// </summary>
[RequirePermission("inventory.edit")]
[RequireFeature("inventory_management")]
public record UpdateProductCommand : IRequest<Result<ProductDetailDto>>, ITenantRequest
{
    public Guid ProductId { get; init; }
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
    public bool IsActive { get; init; }

    /// <summary>
    /// Determines how the update should be synced to the sales channel.
    /// Default is PendingSync (queue for later push).
    /// </summary>
    public ProductSyncMode SyncMode { get; init; } = ProductSyncMode.PendingSync;
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        ITenantDbContext dbContext,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<ProductDetailDto>> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.DeletedAt == null, cancellationToken);

        if (product == null)
        {
            return Result<ProductDetailDto>.Failure("Product not found");
        }

        // Update product
        product.Update(request.Name, request.Description, request.Category, request.Brand);

        var costPrice = new Money(request.CostPrice, request.Currency);
        var sellingPrice = new Money(request.SellingPrice, request.Currency);
        product.UpdatePricing(costPrice, sellingPrice);

        // Set sync status based on sync mode
        switch (request.SyncMode)
        {
            case ProductSyncMode.LocalOnly:
                product.MarkAsLocalOnly();
                break;
            case ProductSyncMode.PendingSync:
                product.MarkAsPendingSync();
                break;
            case ProductSyncMode.SyncImmediately:
                // TODO: Implement channel sync service call here
                // For now, mark as pending until sync service is implemented
                product.MarkAsPendingSync();
                _logger.LogInformation("Immediate sync requested for product {ProductId} - queued for sync", product.Id);
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {ProductId} updated", product.Id);

        // Get inventory data
        var inventoryItems = await _dbContext.Inventory
            .AsNoTracking()
            .Where(i => i.ProductId == product.Id)
            .ToListAsync(cancellationToken);

        // Build variant DTOs
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

        var result = new ProductDetailDto
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
            SyncStatus = product.SyncStatus,
            LastSyncedAt = product.LastSyncedAt,
            ChannelProductId = product.ChannelProductId,
            ChannelSellingPrice = product.ChannelSellingPrice?.Amount,
            ChannelSellingCurrency = product.ChannelSellingPrice?.Currency,
            Variants = variantDtos,
            InventorySummary = new InventorySummaryDto
            {
                TotalOnHand = inventoryItems.Sum(i => i.QuantityOnHand),
                TotalReserved = inventoryItems.Sum(i => i.QuantityReserved),
                TotalAvailable = inventoryItems.Sum(i => i.QuantityAvailable),
                IsLowStock = inventoryItems.Any(i => i.IsLowStock()),
                Items = inventoryItems.Select(i => new InventoryItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    Sku = i.Sku,
                    QuantityOnHand = i.QuantityOnHand,
                    QuantityReserved = i.QuantityReserved,
                    QuantityAvailable = i.QuantityAvailable,
                    ReorderPoint = i.ReorderPoint,
                    ReorderQuantity = i.ReorderQuantity,
                    Location = i.Location,
                    IsLowStock = i.IsLowStock()
                }).ToList()
            }
        };

        return Result<ProductDetailDto>.Success(result);
    }
}
