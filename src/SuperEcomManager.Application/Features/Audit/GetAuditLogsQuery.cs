using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Audit;

namespace SuperEcomManager.Application.Features.Audit;

/// <summary>
/// Query to get paginated audit logs with filters.
/// </summary>
[RequirePermission("audit.view")]
[RequireFeature("audit")]
public record GetAuditLogsQuery : IRequest<Result<PaginatedResult<AuditLogListDto>>>, ITenantRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public AuditModule? Module { get; init; }
    public AuditAction? Action { get; init; }
    public Guid? UserId { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public bool? IsSuccess { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Search { get; init; }
}

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<PaginatedResult<AuditLogListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetAuditLogsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<AuditLogListDto>>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        // Apply filters
        if (request.Module.HasValue)
        {
            query = query.Where(a => a.Module == request.Module.Value);
        }

        if (request.Action.HasValue)
        {
            query = query.Where(a => a.Action == request.Action.Value);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(a => a.EntityType == request.EntityType);
        }

        if (request.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == request.EntityId.Value);
        }

        if (request.IsSuccess.HasValue)
        {
            query = query.Where(a => a.IsSuccess == request.IsSuccess.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(a =>
                a.Description.ToLower().Contains(searchLower) ||
                (a.UserName != null && a.UserName.ToLower().Contains(searchLower)) ||
                (a.EntityType != null && a.EntityType.ToLower().Contains(searchLower)));
        }

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

/// <summary>
/// Query to get a single audit log by ID.
/// </summary>
[RequirePermission("audit.view")]
[RequireFeature("audit")]
public record GetAuditLogByIdQuery : IRequest<Result<AuditLogDetailDto>>, ITenantRequest
{
    public Guid Id { get; init; }
}

public class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, Result<AuditLogDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetAuditLogByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AuditLogDetailDto>> Handle(
        GetAuditLogByIdQuery request,
        CancellationToken cancellationToken)
    {
        var auditLog = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new AuditLogDetailDto
            {
                Id = a.Id,
                Action = a.Action,
                ActionName = AuditEnumHelper.GetActionName(a.Action),
                Module = a.Module,
                ModuleName = AuditEnumHelper.GetModuleName(a.Module),
                UserId = a.UserId,
                UserName = a.UserName,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Description = a.Description,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                AdditionalData = a.AdditionalData,
                IsSuccess = a.IsSuccess,
                ErrorMessage = a.ErrorMessage,
                Timestamp = a.Timestamp
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (auditLog == null)
        {
            return Result<AuditLogDetailDto>.Failure("Audit log not found.");
        }

        return Result<AuditLogDetailDto>.Success(auditLog);
    }
}
