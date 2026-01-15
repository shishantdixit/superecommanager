using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Inventory;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Inventory;

/// <summary>
/// Command to adjust stock for an inventory item.
/// </summary>
[RequirePermission("inventory.adjust")]
[RequireFeature("inventory_management")]
public record AdjustStockCommand : IRequest<Result<InventoryItemDto>>, ITenantRequest
{
    public Guid InventoryItemId { get; init; }
    public MovementType AdjustmentType { get; init; }
    public int Quantity { get; init; }
    public string? Notes { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
}

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Result<InventoryItemDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdjustStockCommandHandler> _logger;

    public AdjustStockCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<AdjustStockCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<InventoryItemDto>> Handle(
        AdjustStockCommand request,
        CancellationToken cancellationToken)
    {
        var inventory = await _dbContext.Inventory
            .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, cancellationToken);

        if (inventory == null)
        {
            return Result<InventoryItemDto>.Failure("Inventory item not found");
        }

        if (request.Quantity <= 0)
        {
            return Result<InventoryItemDto>.Failure("Quantity must be positive");
        }

        // Validate movement type
        var validTypes = new[]
        {
            MovementType.StockIn,
            MovementType.StockOut,
            MovementType.Return,
            MovementType.Adjustment,
            MovementType.Damaged,
            MovementType.Transfer
        };

        if (!validTypes.Contains(request.AdjustmentType))
        {
            return Result<InventoryItemDto>.Failure($"Invalid adjustment type: {request.AdjustmentType}");
        }

        var quantityBefore = inventory.QuantityOnHand;

        try
        {
            // Apply the adjustment
            switch (request.AdjustmentType)
            {
                case MovementType.StockIn:
                case MovementType.Return:
                    inventory.AddStock(request.Quantity);
                    break;

                case MovementType.StockOut:
                case MovementType.Damaged:
                    inventory.RemoveStock(request.Quantity);
                    break;

                case MovementType.Adjustment:
                case MovementType.Transfer:
                    // Adjustment can be positive or negative based on context
                    // For simplicity, positive adds, use StockOut for removal
                    inventory.AddStock(request.Quantity);
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result<InventoryItemDto>.Failure(ex.Message);
        }

        var quantityAfter = inventory.QuantityOnHand;

        // Record the movement
        var movement = StockMovement.Create(
            inventory.Id,
            inventory.Sku,
            request.AdjustmentType,
            request.Quantity,
            quantityBefore,
            quantityAfter,
            _currentUserService.UserId,
            request.ReferenceType,
            request.ReferenceId,
            request.Notes);

        _dbContext.StockMovements.Add(movement);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stock adjusted for {Sku}: {MovementType} {Quantity} units (before: {Before}, after: {After})",
            inventory.Sku, request.AdjustmentType, request.Quantity, quantityBefore, quantityAfter);

        // Get variant name if applicable
        string? variantName = null;
        if (inventory.ProductVariantId.HasValue)
        {
            var variant = await _dbContext.ProductVariants
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == inventory.ProductVariantId, cancellationToken);
            variantName = variant?.Name;
        }

        var dto = new InventoryItemDto
        {
            Id = inventory.Id,
            ProductId = inventory.ProductId,
            ProductVariantId = inventory.ProductVariantId,
            Sku = inventory.Sku,
            VariantName = variantName,
            QuantityOnHand = inventory.QuantityOnHand,
            QuantityReserved = inventory.QuantityReserved,
            QuantityAvailable = inventory.QuantityAvailable,
            ReorderPoint = inventory.ReorderPoint,
            ReorderQuantity = inventory.ReorderQuantity,
            Location = inventory.Location,
            IsLowStock = inventory.IsLowStock()
        };

        return Result<InventoryItemDto>.Success(dto);
    }
}
