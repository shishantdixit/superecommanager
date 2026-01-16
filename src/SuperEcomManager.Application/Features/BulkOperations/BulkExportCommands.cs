using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.BulkOperations;

#region DTOs

/// <summary>
/// Result of bulk export operation.
/// </summary>
public record BulkExportResult<T>
{
    public int TotalRecords { get; init; }
    public List<T> Data { get; init; } = new();
    public DateTime ExportedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Export row for orders.
/// </summary>
public record OrderExportRow
{
    public string OrderNumber { get; init; } = string.Empty;
    public string ExternalOrderId { get; init; } = string.Empty;
    public string? ExternalOrderNumber { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public string ChannelType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string FulfillmentStatus { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }
    public string ShippingName { get; init; } = string.Empty;
    public string ShippingLine1 { get; init; } = string.Empty;
    public string? ShippingLine2 { get; init; }
    public string ShippingCity { get; init; } = string.Empty;
    public string ShippingState { get; init; } = string.Empty;
    public string ShippingPostalCode { get; init; } = string.Empty;
    public string? ShippingPhone { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "INR";
    public bool IsCOD { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Export row for shipments.
/// </summary>
public record ShipmentExportRow
{
    public string ShipmentNumber { get; init; } = string.Empty;
    public string? AwbNumber { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CourierType { get; init; } = string.Empty;
    public string? CourierName { get; init; }
    public string Status { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryState { get; init; } = string.Empty;
    public string DeliveryPostalCode { get; init; } = string.Empty;
    public bool IsCOD { get; init; }
    public decimal? CODAmount { get; init; }
    public DateTime? PickedUpAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Export row for products.
/// </summary>
public record ProductExportRow
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal CostPrice { get; init; }
    public string Currency { get; init; } = "INR";
    public decimal? Weight { get; init; }
    public string? ImageUrl { get; init; }
    public string? HsnCode { get; init; }
    public decimal? TaxRate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Export row for NDR records.
/// </summary>
public record NdrExportRow
{
    public Guid Id { get; init; }
    public string AwbNumber { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string ShipmentNumber { get; init; } = string.Empty;
    public string ReasonCode { get; init; } = string.Empty;
    public string? ReasonDescription { get; init; }
    public string Status { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryState { get; init; } = string.Empty;
    public string? AssignedToUserName { get; init; }
    public int AttemptCount { get; init; }
    public DateTime NdrDate { get; init; }
    public DateTime? NextFollowUpAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? Resolution { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion

#region Export Orders

/// <summary>
/// Command to export orders.
/// </summary>
[RequirePermission("orders.view")]
[RequireFeature("order_management")]
public record ExportOrdersCommand : IRequest<Result<BulkExportResult<OrderExportRow>>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public OrderStatus? Status { get; init; }
    public List<OrderStatus>? Statuses { get; init; }
    public Guid? ChannelId { get; init; }
    public int? MaxRecords { get; init; } = 10000;
}

public class ExportOrdersCommandHandler : IRequestHandler<ExportOrdersCommand, Result<BulkExportResult<OrderExportRow>>>
{
    private readonly ITenantDbContext _dbContext;
    private const int MaxExportSize = 50000;

    public ExportOrdersCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BulkExportResult<OrderExportRow>>> Handle(
        ExportOrdersCommand request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Min(request.MaxRecords ?? MaxExportSize, MaxExportSize);

        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Channel)
            .Where(o => o.DeletedAt == null);

        if (request.FromDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= request.ToDate.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        if (request.Statuses?.Count > 0)
        {
            query = query.Where(o => request.Statuses.Contains(o.Status));
        }

        if (request.ChannelId.HasValue)
        {
            query = query.Where(o => o.ChannelId == request.ChannelId.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var exportData = orders.Select(o => new OrderExportRow
        {
            OrderNumber = o.OrderNumber,
            ExternalOrderId = o.ExternalOrderId,
            ExternalOrderNumber = o.ExternalOrderNumber,
            ChannelName = o.Channel?.Name ?? "",
            ChannelType = o.Channel?.Type.ToString() ?? "",
            Status = o.Status.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(),
            FulfillmentStatus = o.FulfillmentStatus.ToString(),
            CustomerName = o.CustomerName,
            CustomerEmail = o.CustomerEmail,
            CustomerPhone = o.CustomerPhone,
            ShippingName = o.ShippingAddress.Name,
            ShippingLine1 = o.ShippingAddress.Line1,
            ShippingLine2 = o.ShippingAddress.Line2,
            ShippingCity = o.ShippingAddress.City,
            ShippingState = o.ShippingAddress.State,
            ShippingPostalCode = o.ShippingAddress.PostalCode,
            ShippingPhone = o.ShippingAddress.Phone,
            Subtotal = o.Subtotal.Amount,
            DiscountAmount = o.DiscountAmount.Amount,
            TaxAmount = o.TaxAmount.Amount,
            ShippingAmount = o.ShippingAmount.Amount,
            TotalAmount = o.TotalAmount.Amount,
            Currency = o.TotalAmount.Currency,
            IsCOD = o.IsCOD,
            PaymentMethod = o.PaymentMethod?.ToString() ?? "",
            ItemCount = o.Items.Count,
            OrderDate = o.OrderDate,
            ShippedAt = o.ShippedAt,
            DeliveredAt = o.DeliveredAt,
            CreatedAt = o.CreatedAt
        }).ToList();

        return Result<BulkExportResult<OrderExportRow>>.Success(new BulkExportResult<OrderExportRow>
        {
            TotalRecords = exportData.Count,
            Data = exportData
        });
    }
}

#endregion

#region Export Shipments

/// <summary>
/// Command to export shipments.
/// </summary>
[RequirePermission("shipments.view")]
[RequireFeature("shipping_management")]
public record ExportShipmentsCommand : IRequest<Result<BulkExportResult<ShipmentExportRow>>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public ShipmentStatus? Status { get; init; }
    public List<ShipmentStatus>? Statuses { get; init; }
    public CourierType? CourierType { get; init; }
    public int? MaxRecords { get; init; } = 10000;
}

public class ExportShipmentsCommandHandler : IRequestHandler<ExportShipmentsCommand, Result<BulkExportResult<ShipmentExportRow>>>
{
    private readonly ITenantDbContext _dbContext;
    private const int MaxExportSize = 50000;

    public ExportShipmentsCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BulkExportResult<ShipmentExportRow>>> Handle(
        ExportShipmentsCommand request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Min(request.MaxRecords ?? MaxExportSize, MaxExportSize);

        // Join shipments with orders to get order number
        var query = from s in _dbContext.Shipments.AsNoTracking()
                    join o in _dbContext.Orders.AsNoTracking() on s.OrderId equals o.Id into orderJoin
                    from order in orderJoin.DefaultIfEmpty()
                    where s.DeletedAt == null
                    select new { Shipment = s, Order = order };

        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.Shipment.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.Shipment.CreatedAt <= request.ToDate.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Shipment.Status == request.Status.Value);
        }

        if (request.Statuses?.Count > 0)
        {
            query = query.Where(x => request.Statuses.Contains(x.Shipment.Status));
        }

        if (request.CourierType.HasValue)
        {
            query = query.Where(x => x.Shipment.CourierType == request.CourierType.Value);
        }

        var exportData = await query
            .OrderByDescending(x => x.Shipment.CreatedAt)
            .Take(limit)
            .Select(x => new ShipmentExportRow
            {
                ShipmentNumber = x.Shipment.ShipmentNumber,
                AwbNumber = x.Shipment.AwbNumber,
                OrderId = x.Shipment.OrderId,
                OrderNumber = x.Order != null ? x.Order.OrderNumber : "",
                CourierType = x.Shipment.CourierType.ToString(),
                CourierName = x.Shipment.CourierName,
                Status = x.Shipment.Status.ToString(),
                CustomerName = x.Shipment.DeliveryAddress.Name,
                CustomerPhone = x.Shipment.DeliveryAddress.Phone,
                DeliveryCity = x.Shipment.DeliveryAddress.City,
                DeliveryState = x.Shipment.DeliveryAddress.State,
                DeliveryPostalCode = x.Shipment.DeliveryAddress.PostalCode,
                IsCOD = x.Shipment.IsCOD,
                CODAmount = x.Shipment.CODAmount != null ? x.Shipment.CODAmount.Amount : (decimal?)null,
                PickedUpAt = x.Shipment.PickedUpAt,
                DeliveredAt = x.Shipment.DeliveredAt,
                CreatedAt = x.Shipment.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<BulkExportResult<ShipmentExportRow>>.Success(new BulkExportResult<ShipmentExportRow>
        {
            TotalRecords = exportData.Count,
            Data = exportData
        });
    }
}

#endregion

#region Export Products

/// <summary>
/// Command to export products.
/// </summary>
[RequirePermission("inventory.view")]
[RequireFeature("inventory_management")]
public record ExportProductsCommand : IRequest<Result<BulkExportResult<ProductExportRow>>>, ITenantRequest
{
    public string? Category { get; init; }
    public bool? IsActive { get; init; }
    public int? MaxRecords { get; init; } = 10000;
}

public class ExportProductsCommandHandler : IRequestHandler<ExportProductsCommand, Result<BulkExportResult<ProductExportRow>>>
{
    private readonly ITenantDbContext _dbContext;
    private const int MaxExportSize = 50000;

    public ExportProductsCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BulkExportResult<ProductExportRow>>> Handle(
        ExportProductsCommand request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Min(request.MaxRecords ?? MaxExportSize, MaxExportSize);

        var query = _dbContext.Products
            .AsNoTracking()
            .Where(p => p.DeletedAt == null);

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(p => p.Category == request.Category);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        var exportData = await query
            .OrderBy(p => p.Name)
            .Take(limit)
            .Select(p => new ProductExportRow
            {
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category,
                Brand = p.Brand,
                SellingPrice = p.SellingPrice.Amount,
                CostPrice = p.CostPrice.Amount,
                Currency = p.SellingPrice.Currency,
                Weight = p.Weight,
                ImageUrl = p.ImageUrl,
                HsnCode = p.HsnCode,
                TaxRate = p.TaxRate,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<BulkExportResult<ProductExportRow>>.Success(new BulkExportResult<ProductExportRow>
        {
            TotalRecords = exportData.Count,
            Data = exportData
        });
    }
}

#endregion

#region Export NDR

/// <summary>
/// Command to export NDR records.
/// </summary>
[RequirePermission("ndr.view")]
[RequireFeature("ndr_management")]
public record ExportNdrCommand : IRequest<Result<BulkExportResult<NdrExportRow>>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public NdrStatus? Status { get; init; }
    public NdrReasonCode? ReasonCode { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public int? MaxRecords { get; init; } = 10000;
}

public class ExportNdrCommandHandler : IRequestHandler<ExportNdrCommand, Result<BulkExportResult<NdrExportRow>>>
{
    private readonly ITenantDbContext _dbContext;
    private const int MaxExportSize = 50000;

    public ExportNdrCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BulkExportResult<NdrExportRow>>> Handle(
        ExportNdrCommand request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Min(request.MaxRecords ?? MaxExportSize, MaxExportSize);

        // Join NDR records with orders, shipments, and users
        var query = from ndr in _dbContext.NdrRecords.AsNoTracking()
                    join o in _dbContext.Orders.AsNoTracking() on ndr.OrderId equals o.Id into orderJoin
                    from order in orderJoin.DefaultIfEmpty()
                    join s in _dbContext.Shipments.AsNoTracking() on ndr.ShipmentId equals s.Id into shipmentJoin
                    from shipment in shipmentJoin.DefaultIfEmpty()
                    join u in _dbContext.Users.AsNoTracking() on ndr.AssignedToUserId equals u.Id into userJoin
                    from assignedUser in userJoin.DefaultIfEmpty()
                    select new { Ndr = ndr, Order = order, Shipment = shipment, AssignedUser = assignedUser };

        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.Ndr.NdrDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.Ndr.NdrDate <= request.ToDate.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Ndr.Status == request.Status.Value);
        }

        if (request.ReasonCode.HasValue)
        {
            query = query.Where(x => x.Ndr.ReasonCode == request.ReasonCode.Value);
        }

        if (request.AssignedToUserId.HasValue)
        {
            query = query.Where(x => x.Ndr.AssignedToUserId == request.AssignedToUserId.Value);
        }

        var exportData = await query
            .OrderByDescending(x => x.Ndr.NdrDate)
            .Take(limit)
            .Select(x => new NdrExportRow
            {
                Id = x.Ndr.Id,
                AwbNumber = x.Ndr.AwbNumber,
                OrderNumber = x.Order != null ? x.Order.OrderNumber : "",
                ShipmentNumber = x.Shipment != null ? x.Shipment.ShipmentNumber : "",
                ReasonCode = x.Ndr.ReasonCode.ToString(),
                ReasonDescription = x.Ndr.ReasonDescription,
                Status = x.Ndr.Status.ToString(),
                CustomerName = x.Order != null ? x.Order.CustomerName : "",
                CustomerPhone = x.Order != null ? x.Order.CustomerPhone : null,
                DeliveryCity = x.Shipment != null ? x.Shipment.DeliveryAddress.City : "",
                DeliveryState = x.Shipment != null ? x.Shipment.DeliveryAddress.State : "",
                AssignedToUserName = x.AssignedUser != null ? x.AssignedUser.FullName : null,
                AttemptCount = x.Ndr.AttemptCount,
                NdrDate = x.Ndr.NdrDate,
                NextFollowUpAt = x.Ndr.NextFollowUpAt,
                ResolvedAt = x.Ndr.ResolvedAt,
                Resolution = x.Ndr.Resolution,
                CreatedAt = x.Ndr.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<BulkExportResult<NdrExportRow>>.Success(new BulkExportResult<NdrExportRow>
        {
            TotalRecords = exportData.Count,
            Data = exportData
        });
    }
}

#endregion
