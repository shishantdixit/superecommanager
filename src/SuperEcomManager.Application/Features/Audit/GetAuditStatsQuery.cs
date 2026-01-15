using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Audit;

namespace SuperEcomManager.Application.Features.Audit;

/// <summary>
/// Query to get audit statistics.
/// </summary>
[RequirePermission("audit.view")]
[RequireFeature("audit")]
public record GetAuditStatsQuery : IRequest<Result<AuditStatsDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class GetAuditStatsQueryHandler : IRequestHandler<GetAuditStatsQuery, Result<AuditStatsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetAuditStatsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AuditStatsDto>> Handle(
        GetAuditStatsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var query = _dbContext.AuditLogs.AsNoTracking();

        // Apply date filters if provided
        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= request.ToDate.Value);
        }

        // Get basic counts
        var totalActions = await query.CountAsync(cancellationToken);
        var successfulActions = await query.CountAsync(a => a.IsSuccess, cancellationToken);
        var failedActions = totalActions - successfulActions;

        // Get login stats
        var loginActions = new[] { AuditAction.Login, AuditAction.LoginFailed };
        var loginQuery = query.Where(a => loginActions.Contains(a.Action));
        var totalLogins = await loginQuery.CountAsync(cancellationToken);
        var failedLogins = await loginQuery.CountAsync(a => a.Action == AuditAction.LoginFailed, cancellationToken);

        // Get time-based counts (without date filter)
        var allLogs = _dbContext.AuditLogs.AsNoTracking();
        var totalToday = await allLogs.CountAsync(a => a.Timestamp >= today, cancellationToken);
        var totalThisWeek = await allLogs.CountAsync(a => a.Timestamp >= weekStart, cancellationToken);
        var totalThisMonth = await allLogs.CountAsync(a => a.Timestamp >= monthStart, cancellationToken);

        // Activity by module
        var activityByModule = await query
            .GroupBy(a => a.Module)
            .Select(g => new ModuleActivityDto
            {
                Module = g.Key,
                ModuleName = AuditEnumHelper.GetModuleName(g.Key),
                Count = g.Count()
            })
            .OrderByDescending(m => m.Count)
            .ToListAsync(cancellationToken);

        // Top actions
        var topActions = await query
            .GroupBy(a => a.Action)
            .Select(g => new ActionCountDto
            {
                Action = g.Key,
                ActionName = AuditEnumHelper.GetActionName(g.Key),
                Count = g.Count()
            })
            .OrderByDescending(a => a.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        // Most active users
        var mostActiveUsers = await query
            .Where(a => a.UserId != null)
            .GroupBy(a => new { a.UserId, a.UserName })
            .Select(g => new UserActivitySummaryDto
            {
                UserId = g.Key.UserId!.Value,
                UserName = g.Key.UserName ?? "Unknown",
                ActionCount = g.Count(),
                LastActivity = g.Max(a => a.Timestamp)
            })
            .OrderByDescending(u => u.ActionCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        var stats = new AuditStatsDto
        {
            TotalActions = totalActions,
            TotalToday = totalToday,
            TotalThisWeek = totalThisWeek,
            TotalThisMonth = totalThisMonth,
            SuccessfulActions = successfulActions,
            FailedActions = failedActions,
            TotalLogins = totalLogins,
            FailedLogins = failedLogins,
            ActivityByModule = activityByModule,
            TopActions = topActions,
            MostActiveUsers = mostActiveUsers
        };

        return Result<AuditStatsDto>.Success(stats);
    }
}
