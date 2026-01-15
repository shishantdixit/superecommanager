using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Inventory;

/// <summary>
/// Query to get a paginated list of products with optional filtering.
/// </summary>
[RequirePermission("inventory.view")]
[RequireFeature("inventory_management")]
public record GetProductsQuery : IRequest<Result<PaginatedResult<ProductListDto>>>, ITenantRequest
{
    public ProductFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ProductSortBy SortBy { get; init; } = ProductSortBy.Name;
    public bool SortDescending { get; init; } = false;
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PaginatedResult<ProductListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetProductsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<ProductListDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Products
            .Include(p => p.Variants)
            .AsNoTracking()
            .Where(p => p.DeletedAt == null);

        // Apply filters
        if (request.Filter != null)
        {
            var filter = request.Filter;

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Sku.ToLower().Contains(searchTerm) ||
                    p.Name.ToLower().Contains(searchTerm) ||
                    (p.Brand != null && p.Brand.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(p => p.Category == filter.Category);

            if (!string.IsNullOrWhiteSpace(filter.Brand))
                query = query.Where(p => p.Brand == filter.Brand);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.SellingPrice.Amount >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.SellingPrice.Amount <= filter.MaxPrice.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy switch
        {
            ProductSortBy.Name => request.SortDescending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            ProductSortBy.Sku => request.SortDescending
                ? query.OrderByDescending(p => p.Sku)
                : query.OrderBy(p => p.Sku),
            ProductSortBy.Price => request.SortDescending
                ? query.OrderByDescending(p => p.SellingPrice.Amount)
                : query.OrderBy(p => p.SellingPrice.Amount),
            ProductSortBy.CreatedAt => request.SortDescending
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name)
        };

        // Get product IDs for this page
        var products = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Get inventory data for these products
        var productIds = products.Select(p => p.Id).ToList();
        var inventoryData = await _dbContext.Inventory
            .AsNoTracking()
            .Where(i => productIds.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(i => i.QuantityOnHand) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalStock, cancellationToken);

        var items = products.Select(p => new ProductListDto
        {
            Id = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            Category = p.Category,
            Brand = p.Brand,
            CostPrice = p.CostPrice.Amount,
            SellingPrice = p.SellingPrice.Amount,
            Currency = p.SellingPrice.Currency,
            IsActive = p.IsActive,
            ImageUrl = p.ImageUrl,
            TotalStock = inventoryData.GetValueOrDefault(p.Id, 0),
            VariantCount = p.Variants.Count,
            CreatedAt = p.CreatedAt
        }).ToList();

        var result = new PaginatedResult<ProductListDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);

        return Result<PaginatedResult<ProductListDto>>.Success(result);
    }
}
