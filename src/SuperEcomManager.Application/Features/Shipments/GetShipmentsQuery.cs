using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Query to get paginated list of shipments with filters.
/// </summary>
[RequirePermission("shipments.view")]
[RequireFeature("shipping_management")]
public record GetShipmentsQuery : IRequest<Result<PaginatedResult<ShipmentListDto>>>, ITenantRequest
{
    public ShipmentFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ShipmentSortBy SortBy { get; init; } = ShipmentSortBy.CreatedAt;
    public bool SortDescending { get; init; } = true;
}

public class GetShipmentsQueryHandler : IRequestHandler<GetShipmentsQuery, Result<PaginatedResult<ShipmentListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetShipmentsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<ShipmentListDto>>> Handle(
        GetShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = from s in _dbContext.Shipments.AsNoTracking()
                    join o in _dbContext.Orders.AsNoTracking() on s.OrderId equals o.Id into orderJoin
                    from order in orderJoin.DefaultIfEmpty()
                    where s.DeletedAt == null
                    select new { Shipment = s, Order = order };

        // Apply filters
        if (request.Filter != null)
        {
            var filter = request.Filter;

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(x =>
                    x.Shipment.ShipmentNumber.ToLower().Contains(term) ||
                    (x.Shipment.AwbNumber != null && x.Shipment.AwbNumber.ToLower().Contains(term)) ||
                    (x.Order != null && x.Order.OrderNumber.ToLower().Contains(term)) ||
                    (x.Order != null && x.Order.CustomerName.ToLower().Contains(term)));
            }

            if (filter.OrderId.HasValue)
                query = query.Where(x => x.Shipment.OrderId == filter.OrderId.Value);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Shipment.Status == filter.Status.Value);

            if (filter.Statuses?.Count > 0)
                query = query.Where(x => filter.Statuses.Contains(x.Shipment.Status));

            if (filter.CourierType.HasValue)
                query = query.Where(x => x.Shipment.CourierType == filter.CourierType.Value);

            if (filter.IsCOD.HasValue)
                query = query.Where(x => x.Shipment.IsCOD == filter.IsCOD.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(x => x.Shipment.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(x => x.Shipment.CreatedAt <= filter.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(x => x.Shipment.DeliveryAddress.City.ToLower().Contains(filter.City.ToLower()));

            if (!string.IsNullOrWhiteSpace(filter.State))
                query = query.Where(x => x.Shipment.DeliveryAddress.State.ToLower().Contains(filter.State.ToLower()));
        }

        // Apply sorting
        query = request.SortBy switch
        {
            ShipmentSortBy.ExpectedDeliveryDate => request.SortDescending
                ? query.OrderByDescending(x => x.Shipment.ExpectedDeliveryDate)
                : query.OrderBy(x => x.Shipment.ExpectedDeliveryDate),
            ShipmentSortBy.Status => request.SortDescending
                ? query.OrderByDescending(x => x.Shipment.Status)
                : query.OrderBy(x => x.Shipment.Status),
            ShipmentSortBy.CourierType => request.SortDescending
                ? query.OrderByDescending(x => x.Shipment.CourierType)
                : query.OrderBy(x => x.Shipment.CourierType),
            _ => request.SortDescending
                ? query.OrderByDescending(x => x.Shipment.CreatedAt)
                : query.OrderBy(x => x.Shipment.CreatedAt)
        };

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and project
        var shipments = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ShipmentListDto
            {
                Id = x.Shipment.Id,
                OrderId = x.Shipment.OrderId,
                OrderNumber = x.Order != null ? x.Order.OrderNumber : "Unknown",
                ShipmentNumber = x.Shipment.ShipmentNumber,
                AwbNumber = x.Shipment.AwbNumber,
                CourierType = x.Shipment.CourierType,
                CourierName = x.Shipment.CourierName,
                Status = x.Shipment.Status,
                CustomerName = x.Order != null ? x.Order.CustomerName : "Unknown",
                DeliveryCity = x.Shipment.DeliveryAddress.City,
                DeliveryState = x.Shipment.DeliveryAddress.State,
                IsCOD = x.Shipment.IsCOD,
                CODAmount = x.Shipment.CODAmount != null ? x.Shipment.CODAmount.Amount : null,
                ExpectedDeliveryDate = x.Shipment.ExpectedDeliveryDate,
                CreatedAt = x.Shipment.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<ShipmentListDto>(
            shipments,
            totalCount,
            request.Page,
            request.PageSize);

        return Result<PaginatedResult<ShipmentListDto>>.Success(result);
    }
}
