using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Inventory;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Inventory management endpoints.
/// </summary>
[Authorize]
public class InventoryController : ApiControllerBase
{
    /// <summary>
    /// Get a paginated list of products with optional filtering.
    /// </summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductListDto>>>> GetProducts(
        [FromQuery] string? searchTerm,
        [FromQuery] string? category,
        [FromQuery] string? brand,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isLowStock,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] SyncStatus? syncStatus,
        [FromQuery] Guid? channelId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ProductSortBy sortBy = ProductSortBy.Name,
        [FromQuery] bool sortDescending = false)
    {
        var result = await Mediator.Send(new GetProductsQuery
        {
            Filter = new ProductFilterDto
            {
                SearchTerm = searchTerm,
                Category = category,
                Brand = brand,
                IsActive = isActive,
                IsLowStock = isLowStock,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SyncStatus = syncStatus,
                ChannelId = channelId
            },
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDescending = sortDescending
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<ProductListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get product details by ID.
    /// </summary>
    [HttpGet("products/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProductById(Guid id)
    {
        var result = await Mediator.Send(new GetProductByIdQuery { ProductId = id });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<ProductDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<ProductDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Create a new product.
    /// </summary>
    [HttpPost("products")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> CreateProduct(
        [FromBody] CreateProductDto request)
    {
        var result = await Mediator.Send(new CreateProductCommand { Product = request });

        if (result.IsFailure)
            return BadRequestResponse<ProductDetailDto>(string.Join(", ", result.Errors));

        return CreatedResponse($"/api/inventory/products/{result.Value!.Id}", result.Value!);
    }

    /// <summary>
    /// Update a product.
    /// </summary>
    [HttpPut("products/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request)
    {
        var result = await Mediator.Send(new UpdateProductCommand
        {
            ProductId = id,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Brand = request.Brand,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            Currency = request.Currency,
            Weight = request.Weight,
            ImageUrl = request.ImageUrl,
            HsnCode = request.HsnCode,
            TaxRate = request.TaxRate,
            IsActive = request.IsActive,
            SyncMode = request.SyncMode
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<ProductDetailDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<ProductDetailDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Adjust stock for an inventory item.
    /// </summary>
    [HttpPost("stock/adjust")]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InventoryItemDto>>> AdjustStock(
        [FromBody] StockAdjustmentDto request)
    {
        var result = await Mediator.Send(new AdjustStockCommand
        {
            InventoryItemId = request.InventoryItemId,
            AdjustmentType = request.AdjustmentType,
            Quantity = request.Quantity,
            Notes = request.Notes,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId
        });

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFoundResponse<InventoryItemDto>(string.Join(", ", result.Errors));

            return BadRequestResponse<InventoryItemDto>(string.Join(", ", result.Errors));
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get stock movements with optional filtering.
    /// </summary>
    [HttpGet("stock/movements")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<StockMovementDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<StockMovementDto>>>> GetStockMovements(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? inventoryItemId,
        [FromQuery] string? sku,
        [FromQuery] MovementType? movementType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetStockMovementsQuery
        {
            Filter = new StockMovementFilterDto
            {
                ProductId = productId,
                InventoryItemId = inventoryItemId,
                Sku = sku,
                MovementType = movementType,
                FromDate = fromDate,
                ToDate = toDate
            },
            Page = page,
            PageSize = pageSize
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<StockMovementDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get inventory statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<InventoryStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InventoryStatsDto>>> GetInventoryStats()
    {
        var result = await Mediator.Send(new GetInventoryStatsQuery());

        if (result.IsFailure)
            return BadRequestResponse<InventoryStatsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get low stock items.
    /// </summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductListDto>>>> GetLowStockProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetProductsQuery
        {
            Filter = new ProductFilterDto { IsLowStock = true },
            Page = page,
            PageSize = pageSize,
            SortBy = ProductSortBy.Stock,
            SortDescending = false
        });

        if (result.IsFailure)
            return BadRequestResponse<PaginatedResult<ProductListDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }
}

// Request DTOs for the controller
public record UpdateProductRequest
{
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
    /// Sync mode: LocalOnly (0), PendingSync (1), or SyncImmediately (2).
    /// Default is PendingSync.
    /// </summary>
    public ProductSyncMode SyncMode { get; init; } = ProductSyncMode.PendingSync;
}
