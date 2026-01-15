using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Ndr;

/// <summary>
/// Query to get a paginated list of NDR cases with optional filtering.
/// </summary>
[RequirePermission("ndr.view")]
[RequireFeature("ndr_management")]
public record GetNdrCasesQuery : IRequest<Result<PaginatedResult<NdrListDto>>>, ITenantRequest
{
    public NdrFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public NdrSortBy SortBy { get; init; } = NdrSortBy.NdrDate;
    public bool SortDescending { get; init; } = true;
}

public class GetNdrCasesQueryHandler : IRequestHandler<GetNdrCasesQuery, Result<PaginatedResult<NdrListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetNdrCasesQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<NdrListDto>>> Handle(
        GetNdrCasesQuery request,
        CancellationToken cancellationToken)
    {
        // Join NDR records with orders, shipments, and users for complete info
        var query = from ndr in _dbContext.NdrRecords.AsNoTracking()
                    join o in _dbContext.Orders.AsNoTracking() on ndr.OrderId equals o.Id into orderJoin
                    from order in orderJoin.DefaultIfEmpty()
                    join s in _dbContext.Shipments.AsNoTracking() on ndr.ShipmentId equals s.Id into shipmentJoin
                    from shipment in shipmentJoin.DefaultIfEmpty()
                    join u in _dbContext.Users.AsNoTracking() on ndr.AssignedToUserId equals u.Id into userJoin
                    from assignedUser in userJoin.DefaultIfEmpty()
                    select new { Ndr = ndr, Order = order, Shipment = shipment, AssignedUser = assignedUser };

        // Apply filters
        if (request.Filter != null)
        {
            var filter = request.Filter;

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(x =>
                    x.Ndr.AwbNumber.ToLower().Contains(searchTerm) ||
                    (x.Order != null && x.Order.OrderNumber.ToLower().Contains(searchTerm)) ||
                    (x.Order != null && x.Order.CustomerName.ToLower().Contains(searchTerm)) ||
                    (x.Order != null && x.Order.CustomerPhone != null && x.Order.CustomerPhone.Contains(searchTerm)));
            }

            if (filter.OrderId.HasValue)
                query = query.Where(x => x.Ndr.OrderId == filter.OrderId.Value);

            if (filter.ShipmentId.HasValue)
                query = query.Where(x => x.Ndr.ShipmentId == filter.ShipmentId.Value);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Ndr.Status == filter.Status.Value);

            if (filter.Statuses != null && filter.Statuses.Count > 0)
                query = query.Where(x => filter.Statuses.Contains(x.Ndr.Status));

            if (filter.ReasonCode.HasValue)
                query = query.Where(x => x.Ndr.ReasonCode == filter.ReasonCode.Value);

            if (filter.AssignedToUserId.HasValue)
                query = query.Where(x => x.Ndr.AssignedToUserId == filter.AssignedToUserId.Value);

            if (filter.Unassigned == true)
                query = query.Where(x => x.Ndr.AssignedToUserId == null);

            if (filter.HasFollowUpDue == true)
                query = query.Where(x => x.Ndr.NextFollowUpAt != null && x.Ndr.NextFollowUpAt <= DateTime.UtcNow);

            if (filter.FromDate.HasValue)
                query = query.Where(x => x.Ndr.NdrDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(x => x.Ndr.NdrDate <= filter.ToDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy switch
        {
            NdrSortBy.NdrDate => request.SortDescending
                ? query.OrderByDescending(x => x.Ndr.NdrDate)
                : query.OrderBy(x => x.Ndr.NdrDate),
            NdrSortBy.CreatedAt => request.SortDescending
                ? query.OrderByDescending(x => x.Ndr.CreatedAt)
                : query.OrderBy(x => x.Ndr.CreatedAt),
            NdrSortBy.NextFollowUpAt => request.SortDescending
                ? query.OrderByDescending(x => x.Ndr.NextFollowUpAt)
                : query.OrderBy(x => x.Ndr.NextFollowUpAt),
            NdrSortBy.Status => request.SortDescending
                ? query.OrderByDescending(x => x.Ndr.Status)
                : query.OrderBy(x => x.Ndr.Status),
            NdrSortBy.AttemptCount => request.SortDescending
                ? query.OrderByDescending(x => x.Ndr.AttemptCount)
                : query.OrderBy(x => x.Ndr.AttemptCount),
            _ => query.OrderByDescending(x => x.Ndr.NdrDate)
        };

        // Apply pagination
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new NdrListDto
            {
                Id = x.Ndr.Id,
                ShipmentId = x.Ndr.ShipmentId,
                OrderId = x.Ndr.OrderId,
                OrderNumber = x.Order != null ? x.Order.OrderNumber : "Unknown",
                AwbNumber = x.Ndr.AwbNumber,
                Status = x.Ndr.Status,
                ReasonCode = x.Ndr.ReasonCode,
                ReasonDescription = x.Ndr.ReasonDescription,
                NdrDate = x.Ndr.NdrDate,
                AssignedToUserName = x.AssignedUser != null ? x.AssignedUser.FullName : null,
                AttemptCount = x.Ndr.AttemptCount,
                NextFollowUpAt = x.Ndr.NextFollowUpAt,
                CustomerName = x.Order != null ? x.Order.CustomerName : "Unknown",
                CustomerPhone = x.Order != null ? x.Order.CustomerPhone : null,
                DeliveryCity = x.Shipment != null ? x.Shipment.DeliveryAddress.City : "",
                CreatedAt = x.Ndr.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<NdrListDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);

        return Result<PaginatedResult<NdrListDto>>.Success(result);
    }
}
