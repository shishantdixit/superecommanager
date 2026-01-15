using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Audit;

namespace SuperEcomManager.Application.Features.Audit;

/// <summary>
/// Query to get activity for a specific user.
/// </summary>
[RequirePermission("audit.view")]
[RequireFeature("audit")]
public record GetUserActivityQuery : IRequest<Result<PaginatedResult<UserActivityDto>>>, ITenantRequest
{
    public Guid UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public AuditModule? Module { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class GetUserActivityQueryHandler : IRequestHandler<GetUserActivityQuery, Result<PaginatedResult<UserActivityDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetUserActivityQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<UserActivityDto>>> Handle(
        GetUserActivityQuery request,
        CancellationToken cancellationToken)
    {
        // Validate user exists
        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            return Result<PaginatedResult<UserActivityDto>>.Failure("User not found.");
        }

        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == request.UserId);

        // Apply filters
        if (request.Module.HasValue)
        {
            query = query.Where(a => a.Module == request.Module.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= request.ToDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated items
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new UserActivityDto
            {
                Id = a.Id,
                Action = a.Action,
                ActionName = AuditEnumHelper.GetActionName(a.Action),
                Module = a.Module,
                ModuleName = AuditEnumHelper.GetModuleName(a.Module),
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Description = a.Description,
                IsSuccess = a.IsSuccess,
                Timestamp = a.Timestamp
            })
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<UserActivityDto>(items, totalCount, request.Page, request.PageSize);
        return Result<PaginatedResult<UserActivityDto>>.Success(result);
    }
}

/// <summary>
/// Query to get entity-specific audit history.
/// </summary>
[RequirePermission("audit.view")]
[RequireFeature("audit")]
public record GetEntityAuditHistoryQuery : IRequest<Result<PaginatedResult<AuditLogListDto>>>, ITenantRequest
{
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetEntityAuditHistoryQueryHandler : IRequestHandler<GetEntityAuditHistoryQuery, Result<PaginatedResult<AuditLogListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetEntityAuditHistoryQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<AuditLogListDto>>> Handle(
        GetEntityAuditHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == request.EntityType && a.EntityId == request.EntityId);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated items
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogListDto
            {
                Id = a.Id,
                Action = a.Action,
                ActionName = AuditEnumHelper.GetActionName(a.Action),
                Module = a.Module,
                ModuleName = AuditEnumHelper.GetModuleName(a.Module),
                UserId = a.UserId,
                UserName = a.UserName,
                IpAddress = a.IpAddress,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Description = a.Description,
                IsSuccess = a.IsSuccess,
                Timestamp = a.Timestamp
            })
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<AuditLogListDto>(items, totalCount, request.Page, request.PageSize);
        return Result<PaginatedResult<AuditLogListDto>>.Success(result);
    }
}
