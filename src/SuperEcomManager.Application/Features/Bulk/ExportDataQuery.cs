using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Bulk;

/// <summary>
/// Query to export orders to CSV.
/// </summary>
[RequirePermission("orders.export")]
[RequireFeature("orders")]
public record ExportOrdersQuery : IRequest<Result<CsvExportResultDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public OrderStatus? StatusFilter { get; init; }
    public Guid? ChannelFilter { get; init; }
    public int? Limit { get; init; } = 10000;
}

public class ExportOrdersQueryHandler : IRequestHandler<ExportOrdersQuery, Result<CsvExportResultDto>>
{
    private readonly ITenantDbContext _dbContext;

    public ExportOrdersQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CsvExportResultDto>> Handle(
        ExportOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(o => o.OrderDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(o => o.OrderDate <= request.ToDate.Value);

        if (request.StatusFilter.HasValue)
            query = query.Where(o => o.Status == request.StatusFilter.Value);

        if (request.ChannelFilter.HasValue)
            query = query.Where(o => o.ChannelId == request.ChannelFilter.Value);

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Take(request.Limit ?? 10000)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();

        // Header
        csv.AppendLine("OrderNumber,ExternalOrderId,OrderDate,Status,CustomerName,CustomerEmail,CustomerPhone,ShippingAddress,ShippingCity,ShippingState,ShippingPostalCode,Subtotal,Discount,Tax,Shipping,Total,PaymentMethod,PaymentStatus,ItemCount");

        // Data rows
        foreach (var order in orders)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(order.OrderNumber),
                EscapeCsv(order.ExternalOrderId),
                order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                order.Status.ToString(),
                EscapeCsv(order.CustomerName),
                EscapeCsv(order.CustomerEmail ?? ""),
                EscapeCsv(order.CustomerPhone ?? ""),
                EscapeCsv(order.ShippingAddress.Line1),
                EscapeCsv(order.ShippingAddress.City),
                EscapeCsv(order.ShippingAddress.State),
                EscapeCsv(order.ShippingAddress.PostalCode),
                order.Subtotal.Amount.ToString("F2"),
                order.DiscountAmount.Amount.ToString("F2"),
                order.TaxAmount.Amount.ToString("F2"),
                order.ShippingAmount.Amount.ToString("F2"),
                order.TotalAmount.Amount.ToString("F2"),
                order.PaymentMethod?.ToString() ?? "",
                order.PaymentStatus.ToString(),
                order.Items.Count.ToString()
            ));
        }

        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"orders_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        var result = new CsvExportResultDto
        {
            FileName = fileName,
            Content = content,
            ContentType = "text/csv",
            RowCount = orders.Count
        };

        return Result<CsvExportResultDto>.Success(result);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

/// <summary>
/// Query to export shipments to CSV.
/// </summary>
[RequirePermission("shipments.export")]
[RequireFeature("shipments")]
public record ExportShipmentsQuery : IRequest<Result<CsvExportResultDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public ShipmentStatus? StatusFilter { get; init; }
    public CourierType? CourierFilter { get; init; }
    public int? Limit { get; init; } = 10000;
}

public class ExportShipmentsQueryHandler : IRequestHandler<ExportShipmentsQuery, Result<CsvExportResultDto>>
{
    private readonly ITenantDbContext _dbContext;

    public ExportShipmentsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CsvExportResultDto>> Handle(
        ExportShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Shipments
            .AsNoTracking()
            .AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);

        if (request.StatusFilter.HasValue)
            query = query.Where(s => s.Status == request.StatusFilter.Value);

        if (request.CourierFilter.HasValue)
            query = query.Where(s => s.CourierType == request.CourierFilter.Value);

        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(request.Limit ?? 10000)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();

        // Header
        csv.AppendLine("ShipmentNumber,AWB,CourierType,Status,CreatedAt,PickedUpAt,DeliveredAt,DeliveryCity,DeliveryState,DeliveryPostalCode,IsCOD,CODAmount,ShippingCost");

        foreach (var shipment in shipments)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(shipment.ShipmentNumber),
                EscapeCsv(shipment.AwbNumber ?? ""),
                shipment.CourierType.ToString(),
                shipment.Status.ToString(),
                shipment.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                shipment.PickedUpAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                shipment.DeliveredAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                EscapeCsv(shipment.DeliveryAddress?.City ?? ""),
                EscapeCsv(shipment.DeliveryAddress?.State ?? ""),
                EscapeCsv(shipment.DeliveryAddress?.PostalCode ?? ""),
                shipment.IsCOD.ToString(),
                shipment.CODAmount?.Amount.ToString("F2") ?? "",
                shipment.ShippingCost?.Amount.ToString("F2") ?? ""
            ));
        }

        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"shipments_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        var result = new CsvExportResultDto
        {
            FileName = fileName,
            Content = content,
            ContentType = "text/csv",
            RowCount = shipments.Count
        };

        return Result<CsvExportResultDto>.Success(result);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

/// <summary>
/// Query to export products/inventory to CSV.
/// </summary>
[RequirePermission("inventory.export")]
[RequireFeature("inventory")]
public record ExportProductsQuery : IRequest<Result<CsvExportResultDto>>, ITenantRequest
{
    public bool IncludeLowStock { get; init; } = false;
    public bool IncludeOutOfStock { get; init; } = false;
    public int? Limit { get; init; } = 10000;
}

public class ExportProductsQueryHandler : IRequestHandler<ExportProductsQuery, Result<CsvExportResultDto>>
{
    private readonly ITenantDbContext _dbContext;

    public ExportProductsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CsvExportResultDto>> Handle(
        ExportProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .AsQueryable();

        var products = await query
            .OrderBy(p => p.Name)
            .Take(request.Limit ?? 10000)
            .ToListAsync(cancellationToken);

        // Get inventory for all products
        var productIds = products.Select(p => p.Id).ToList();
        var inventory = await _dbContext.Inventory
            .AsNoTracking()
            .Where(i => productIds.Contains(i.ProductId))
            .ToListAsync(cancellationToken);

        var inventoryByProduct = inventory
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.QuantityAvailable));

        var reorderPointByProduct = inventory
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Max(i => i.ReorderPoint));

        var csv = new StringBuilder();

        // Header
        csv.AppendLine("SKU,Name,Description,Price,CostPrice,StockQuantity,ReorderLevel,Category,Weight,IsActive");

        foreach (var product in products)
        {
            var stock = inventoryByProduct.GetValueOrDefault(product.Id, 0);
            var reorderPoint = reorderPointByProduct.GetValueOrDefault(product.Id, 0);

            // Apply filters
            if (request.IncludeLowStock && stock > reorderPoint)
                continue;
            if (request.IncludeOutOfStock && stock > 0)
                continue;

            csv.AppendLine(string.Join(",",
                EscapeCsv(product.Sku),
                EscapeCsv(product.Name),
                EscapeCsv(product.Description ?? ""),
                product.SellingPrice.Amount.ToString("F2"),
                product.CostPrice?.Amount.ToString("F2") ?? "",
                stock.ToString(),
                reorderPoint.ToString(),
                EscapeCsv(product.Category ?? ""),
                product.Weight?.ToString("F2") ?? "",
                product.IsActive.ToString()
            ));
        }

        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"products_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        var result = new CsvExportResultDto
        {
            FileName = fileName,
            Content = content,
            ContentType = "text/csv",
            RowCount = products.Count
        };

        return Result<CsvExportResultDto>.Success(result);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
