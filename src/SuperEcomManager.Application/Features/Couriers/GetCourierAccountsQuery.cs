using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Query to get all courier accounts for the current tenant.
/// </summary>
public record GetCourierAccountsQuery : IRequest<List<CourierAccountDto>>, ITenantRequest
{
    public bool? IsActive { get; init; }
}

public class GetCourierAccountsQueryHandler : IRequestHandler<GetCourierAccountsQuery, List<CourierAccountDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetCourierAccountsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CourierAccountDto>> Handle(
        GetCourierAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.CourierAccounts.AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == request.IsActive.Value);
        }

        return await query
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Name)
            .Select(c => new CourierAccountDto
            {
                Id = c.Id,
                Name = c.Name,
                CourierType = c.CourierType,
                IsActive = c.IsActive,
                IsDefault = c.IsDefault,
                IsConnected = c.IsConnected,
                LastConnectedAt = c.LastConnectedAt,
                LastError = c.LastError,
                Priority = c.Priority,
                SupportsCOD = c.SupportsCOD,
                SupportsReverse = c.SupportsReverse,
                SupportsExpress = c.SupportsExpress,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
