using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Audit;

namespace SuperEcomManager.Application.Features.Audit;

/// <summary>
/// Query to get login history with filters.
/// </summary>
[RequirePermission("audit.view")]
[RequireFeature("audit")]
public record GetLoginHistoryQuery : IRequest<Result<PaginatedResult<LoginHistoryDto>>>, ITenantRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? UserId { get; init; }
    public bool? IsSuccess { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? IpAddress { get; init; }
}

public class GetLoginHistoryQueryHandler : IRequestHandler<GetLoginHistoryQuery, Result<PaginatedResult<LoginHistoryDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetLoginHistoryQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<LoginHistoryDto>>> Handle(
        GetLoginHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // Filter for login-related actions only
        var loginActions = new[] { AuditAction.Login, AuditAction.Logout, AuditAction.LoginFailed };

        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => loginActions.Contains(a.Action));

        // Apply filters
        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId.Value);
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

        if (!string.IsNullOrWhiteSpace(request.IpAddress))
        {
            query = query.Where(a => a.IpAddress == request.IpAddress);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated items
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new LoginHistoryDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.UserName,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                IsSuccess = a.IsSuccess,
                ErrorMessage = a.ErrorMessage,
                Timestamp = a.Timestamp,
                Action = a.Action,
                ActionName = AuditEnumHelper.GetActionName(a.Action)
            })
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<LoginHistoryDto>(items, totalCount, request.Page, request.PageSize);
        return Result<PaginatedResult<LoginHistoryDto>>.Success(result);
    }
}
