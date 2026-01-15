using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Inventory;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Application.Features.Inventory;

/// <summary>
/// Command to create a new product.
/// </summary>
[RequirePermission("inventory.create")]
[RequireFeature("inventory_management")]
public record CreateProductCommand : IRequest<Result<ProductDetailDto>>, ITenantRequest
{
    public CreateProductDto Product { get; init; } = null!;
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<CreateProductCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<ProductDetailDto>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Product;

        // Validate SKU is unique
        var existingSku = await _dbContext.Products
            .AnyAsync(p => p.Sku == dto.Sku.Trim().ToUpperInvariant() && p.DeletedAt == null, cancellationToken);

        if (existingSku)
        {
            return Result<ProductDetailDto>.Failure($"Product with SKU '{dto.Sku}' already exists");
        }

        // Create product
        var costPrice = new Money(dto.CostPrice, dto.Currency);
        var sellingPrice = new Money(dto.SellingPrice, dto.Currency);

        var product = Product.Create(dto.Sku, dto.Name, costPrice, sellingPrice);
        product.Update(dto.Name, dto.Description, dto.Category, dto.Brand);

        _dbContext.Products.Add(product);

        // Create inventory item for main product
        var mainInventory = InventoryItem.Create(product.Id, product.Sku);
        if (dto.InitialStock > 0)
        {
            mainInventory.AddStock(dto.InitialStock);
        }
        _dbContext.Inventory.Add(mainInventory);

        // Create initial stock movement if there's stock
        if (dto.InitialStock > 0)
        {
            var movement = StockMovement.Create(
                mainInventory.Id,
                product.Sku,
                MovementType.InitialStock,
                dto.InitialStock,
                0,
                dto.InitialStock,
                _currentUserService.UserId,
                "Product",
                product.Id.ToString(),
                "Initial stock on product creation");
            _dbContext.StockMovements.Add(movement);
        }

        // Create variants if provided
        var variantDtos = new List<ProductVariantDto>();
        var inventoryItems = new List<InventoryItem> { mainInventory };

        if (dto.Variants != null && dto.Variants.Count > 0)
        {
            foreach (var variantDto in dto.Variants)
            {
                // Check variant SKU is unique
                var existingVariantSku = await _dbContext.ProductVariants
                    .AnyAsync(v => v.Sku == variantDto.Sku.Trim().ToUpperInvariant(), cancellationToken);

                if (existingVariantSku)
                {
                    return Result<ProductDetailDto>.Failure($"Variant with SKU '{variantDto.Sku}' already exists");
                }

                var variant = ProductVariant.Create(product.Id, variantDto.Sku, variantDto.Name);
                variant.SetOptions(variantDto.Option1Name, variantDto.Option1Value, variantDto.Option2Name, variantDto.Option2Value);
                product.AddVariant(variant);

                // Create inventory for variant
                var variantInventory = InventoryItem.Create(product.Id, variant.Sku, variant.Id);
                if (variantDto.InitialStock > 0)
                {
                    variantInventory.AddStock(variantDto.InitialStock);

                    var variantMovement = StockMovement.Create(
                        variantInventory.Id,
                        variant.Sku,
                        MovementType.InitialStock,
                        variantDto.InitialStock,
                        0,
                        variantDto.InitialStock,
                        _currentUserService.UserId,
                        "Variant",
                        variant.Id.ToString(),
                        "Initial stock on variant creation");
                    _dbContext.StockMovements.Add(variantMovement);
                }
                _dbContext.Inventory.Add(variantInventory);
                inventoryItems.Add(variantInventory);

                variantDtos.Add(new ProductVariantDto
                {
                    Id = variant.Id,
                    Sku = variant.Sku,
                    Name = variant.Name,
                    Option1Name = variant.Option1Name,
                    Option1Value = variant.Option1Value,
                    Option2Name = variant.Option2Name,
                    Option2Value = variant.Option2Value,
                    CostPrice = variantDto.CostPrice,
                    SellingPrice = variantDto.SellingPrice,
                    Weight = variantDto.Weight,
                    ImageUrl = variantDto.ImageUrl,
                    IsActive = true,
                    QuantityOnHand = variantDto.InitialStock,
                    QuantityAvailable = variantDto.InitialStock
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product {ProductId} created with SKU {Sku} by user {UserId}",
            product.Id, product.Sku, _currentUserService.UserId);

        // Build response
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
            Variants = variantDtos,
            InventorySummary = new InventorySummaryDto
            {
                TotalOnHand = inventoryItems.Sum(i => i.QuantityOnHand),
                TotalReserved = 0,
                TotalAvailable = inventoryItems.Sum(i => i.QuantityOnHand),
                IsLowStock = false,
                Items = inventoryItems.Select(i => new InventoryItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    Sku = i.Sku,
                    QuantityOnHand = i.QuantityOnHand,
                    QuantityReserved = 0,
                    QuantityAvailable = i.QuantityOnHand,
                    ReorderPoint = i.ReorderPoint,
                    ReorderQuantity = i.ReorderQuantity,
                    IsLowStock = i.IsLowStock()
                }).ToList()
            }
        };

        return Result<ProductDetailDto>.Success(result);
    }
}
