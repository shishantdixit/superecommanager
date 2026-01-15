using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Query to get paginated list of orders with filters.
/// </summary>
[RequirePermission("orders.view")]
[RequireFeature("order_management")]
public record GetOrdersQuery : IRequest<Result<PaginatedResult<OrderListDto>>>, ITenantRequest
{
    public OrderFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public OrderSortBy SortBy { get; init; } = OrderSortBy.OrderDate;
    public bool SortDescending { get; init; } = true;
}

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, Result<PaginatedResult<OrderListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetOrdersQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<OrderListDto>>> Handle(
        GetOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Orders
            .Include(o => o.Channel)
            .Include(o => o.Items)
            .AsNoTracking()
            .Where(o => o.DeletedAt == null);

        // Apply filters
        if (request.Filter != null)
        {
            query = ApplyFilters(query, request.Filter);
        }

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortDescending);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var orders = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderListDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                ExternalOrderNumber = o.ExternalOrderNumber,
                ChannelName = o.Channel != null ? o.Channel.Name : "Unknown",
                ChannelType = o.Channel != null ? o.Channel.Type : ChannelType.Custom,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                FulfillmentStatus = o.FulfillmentStatus,
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                ShippingCity = o.ShippingAddress.City,
                ShippingState = o.ShippingAddress.State,
                TotalAmount = o.TotalAmount.Amount,
                Currency = o.TotalAmount.Currency,
                IsCOD = o.PaymentMethod == PaymentMethod.COD,
                ItemCount = o.Items.Count,
                OrderDate = o.OrderDate,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<OrderListDto>(
            orders,
            totalCount,
            request.Page,
            request.PageSize);

        return Result<PaginatedResult<OrderListDto>>.Success(result);
    }

    private static IQueryable<Domain.Entities.Orders.Order> ApplyFilters(
        IQueryable<Domain.Entities.Orders.Order> query,
        OrderFilterDto filter)
    {
        // Search term - searches order number, customer name, phone, external order number
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(term) ||
                o.CustomerName.ToLower().Contains(term) ||
                (o.CustomerPhone != null && o.CustomerPhone.Contains(term)) ||
                (o.ExternalOrderNumber != null && o.ExternalOrderNumber.ToLower().Contains(term)) ||
                o.ExternalOrderId.ToLower().Contains(term));
        }

        // Single status filter
        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        // Multiple status filter
        if (filter.Statuses?.Count > 0)
        {
            query = query.Where(o => filter.Statuses.Contains(o.Status));
        }

        // Payment status
        if (filter.PaymentStatus.HasValue)
        {
            query = query.Where(o => o.PaymentStatus == filter.PaymentStatus.Value);
        }

        // Fulfillment status
        if (filter.FulfillmentStatus.HasValue)
        {
            query = query.Where(o => o.FulfillmentStatus == filter.FulfillmentStatus.Value);
        }

        // Channel ID
        if (filter.ChannelId.HasValue)
        {
            query = query.Where(o => o.ChannelId == filter.ChannelId.Value);
        }

        // Channel type
        if (filter.ChannelType.HasValue)
        {
            query = query.Where(o => o.Channel != null && o.Channel.Type == filter.ChannelType.Value);
        }

        // COD filter
        if (filter.IsCOD.HasValue)
        {
            if (filter.IsCOD.Value)
            {
                query = query.Where(o => o.PaymentMethod == PaymentMethod.COD);
            }
            else
            {
                query = query.Where(o => o.PaymentMethod != PaymentMethod.COD);
            }
        }

        // Date range
        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= filter.ToDate.Value);
        }

        // Location filters
        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            query = query.Where(o => o.ShippingAddress.City.ToLower().Contains(filter.City.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filter.State))
        {
            query = query.Where(o => o.ShippingAddress.State.ToLower().Contains(filter.State.ToLower()));
        }

        // Amount range
        if (filter.MinAmount.HasValue)
        {
            query = query.Where(o => o.TotalAmount.Amount >= filter.MinAmount.Value);
        }

        if (filter.MaxAmount.HasValue)
        {
            query = query.Where(o => o.TotalAmount.Amount <= filter.MaxAmount.Value);
        }

        return query;
    }

    private static IQueryable<Domain.Entities.Orders.Order> ApplySorting(
        IQueryable<Domain.Entities.Orders.Order> query,
        OrderSortBy sortBy,
        bool descending)
    {
        return sortBy switch
        {
            OrderSortBy.OrderDate => descending
                ? query.OrderByDescending(o => o.OrderDate)
                : query.OrderBy(o => o.OrderDate),
            OrderSortBy.CreatedAt => descending
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt),
            OrderSortBy.TotalAmount => descending
                ? query.OrderByDescending(o => o.TotalAmount.Amount)
                : query.OrderBy(o => o.TotalAmount.Amount),
            OrderSortBy.CustomerName => descending
                ? query.OrderByDescending(o => o.CustomerName)
                : query.OrderBy(o => o.CustomerName),
            OrderSortBy.Status => descending
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            _ => query.OrderByDescending(o => o.OrderDate)
        };
    }
}
