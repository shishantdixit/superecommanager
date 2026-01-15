using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Inventory;

/// <summary>
/// Query to get stock movements with filtering.
/// </summary>
[RequirePermission("inventory.view")]
[RequireFeature("inventory_management")]
public record GetStockMovementsQuery : IRequest<Result<PaginatedResult<StockMovementDto>>>, ITenantRequest
{
    public StockMovementFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, Result<PaginatedResult<StockMovementDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetStockMovementsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<StockMovementDto>>> Handle(
        GetStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.StockMovements.AsNoTracking();

        // Apply filters
        if (request.Filter != null)
        {
            var filter = request.Filter;

            if (filter.InventoryItemId.HasValue)
                query = query.Where(m => m.InventoryId == filter.InventoryItemId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Sku))
                query = query.Where(m => m.Sku == filter.Sku);

            if (filter.MovementType.HasValue)
                query = query.Where(m => m.MovementType == filter.MovementType.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(m => m.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(m => m.CreatedAt <= filter.ToDate.Value);

            if (filter.ProductId.HasValue)
            {
                // Get inventory IDs for product
                var inventoryIds = await _dbContext.Inventory
                    .AsNoTracking()
                    .Where(i => i.ProductId == filter.ProductId.Value)
                    .Select(i => i.Id)
                    .ToListAsync(cancellationToken);

                query = query.Where(m => inventoryIds.Contains(m.InventoryId));
            }
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get movements with user info
        var movements = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Get user names
        var userIds = movements
            .Where(m => m.PerformedByUserId.HasValue)
            .Select(m => m.PerformedByUserId!.Value)
            .Distinct()
            .ToList();

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var items = movements.Select(m => new StockMovementDto
        {
            Id = m.Id,
            InventoryId = m.InventoryId,
            Sku = m.Sku,
            MovementType = m.MovementType,
            Quantity = m.Quantity,
            QuantityBefore = m.QuantityBefore,
            QuantityAfter = m.QuantityAfter,
            ReferenceType = m.ReferenceType,
            ReferenceId = m.ReferenceId,
            Notes = m.Notes,
            PerformedByUserName = m.PerformedByUserId.HasValue
                ? users.GetValueOrDefault(m.PerformedByUserId.Value, "Unknown")
                : null,
            CreatedAt = m.CreatedAt
        }).ToList();

        var result = new PaginatedResult<StockMovementDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);

        return Result<PaginatedResult<StockMovementDto>>.Success(result);
    }
}
