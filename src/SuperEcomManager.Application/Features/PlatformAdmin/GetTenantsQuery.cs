using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.PlatformAdmin;

/// <summary>
/// Query to list tenants with filtering and pagination.
/// </summary>
public class GetTenantsQuery : IRequest<PaginatedResult<TenantSummaryDto>>
{
    public string? SearchTerm { get; set; }
    public TenantStatus? Status { get; set; }
    public string? PlanCode { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public bool? IsTrialActive { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, PaginatedResult<TenantSummaryDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetTenantsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResult<TenantSummaryDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.DeletedAt == null);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(search) ||
                t.Slug.ToLower().Contains(search) ||
                (t.CompanyName != null && t.CompanyName.ToLower().Contains(search)) ||
                (t.ContactEmail != null && t.ContactEmail.ToLower().Contains(search)));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.CreatedFrom.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= request.CreatedFrom.Value);
        }

        if (request.CreatedTo.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= request.CreatedTo.Value);
        }

        if (request.IsTrialActive.HasValue)
        {
            var now = DateTime.UtcNow;
            if (request.IsTrialActive.Value)
            {
                query = query.Where(t => t.TrialEndsAt != null && t.TrialEndsAt > now);
            }
            else
            {
                query = query.Where(t => t.TrialEndsAt == null || t.TrialEndsAt <= now);
            }
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "name" => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
            "status" => request.SortDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "companyname" => request.SortDescending ? query.OrderByDescending(t => t.CompanyName) : query.OrderBy(t => t.CompanyName),
            _ => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
        };

        // Apply pagination
        var tenants = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TenantSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                CompanyName = t.CompanyName,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Get subscription info for each tenant
        var tenantIds = tenants.Select(t => t.Id).ToList();
        var activeStatuses = new[] { SubscriptionStatus.Active, SubscriptionStatus.Trial };
        var subscriptions = await _dbContext.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => tenantIds.Contains(s.TenantId) && activeStatuses.Contains(s.Status))
            .Select(s => new { s.TenantId, PlanName = s.Plan!.Name })
            .ToListAsync(cancellationToken);

        var subscriptionMap = subscriptions.ToDictionary(s => s.TenantId, s => s.PlanName);

        foreach (var tenant in tenants)
        {
            tenant.CurrentPlan = subscriptionMap.GetValueOrDefault(tenant.Id);
        }

        return new PaginatedResult<TenantSummaryDto>(
            tenants,
            totalCount,
            request.Page,
            request.PageSize);
    }
}

/// <summary>
/// Query to get a single tenant by ID.
/// </summary>
public class GetTenantByIdQuery : IRequest<Result<TenantAdminDto>>
{
    public Guid TenantId { get; set; }
}

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, Result<TenantAdminDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetTenantByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<TenantAdminDto>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant == null)
        {
            return Result<TenantAdminDto>.Failure("Tenant not found");
        }

        var activeStatuses = new[] { SubscriptionStatus.Active, SubscriptionStatus.Trial };
        var subscription = await _dbContext.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && activeStatuses.Contains(s.Status), cancellationToken);

        var dto = new TenantAdminDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            CompanyName = tenant.CompanyName,
            ContactEmail = tenant.ContactEmail,
            ContactPhone = tenant.ContactPhone,
            SchemaName = tenant.SchemaName,
            Status = tenant.Status,
            TrialEndsAt = tenant.TrialEndsAt,
            IsTrialActive = tenant.IsTrialActive(),
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt,
            CurrentPlan = subscription?.Plan?.Name,
            SubscriptionEndsAt = subscription?.EndDate
        };

        return Result<TenantAdminDto>.Success(dto);
    }
}

/// <summary>
/// Query to get tenant activity logs.
/// </summary>
public class GetTenantActivityLogsQuery : IRequest<PaginatedResult<TenantActivityLogDto>>
{
    public Guid? TenantId { get; set; }
    public string? Action { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetTenantActivityLogsQueryHandler : IRequestHandler<GetTenantActivityLogsQuery, PaginatedResult<TenantActivityLogDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetTenantActivityLogsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResult<TenantActivityLogDto>> Handle(
        GetTenantActivityLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.TenantActivityLogs.AsNoTracking();

        if (request.TenantId.HasValue)
        {
            query = query.Where(l => l.TenantId == request.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(l => l.Action == request.Action);
        }

        if (request.From.HasValue)
        {
            query = query.Where(l => l.PerformedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(l => l.PerformedAt <= request.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(l => l.PerformedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Get tenant and admin names
        var tenantIds = logs.Select(l => l.TenantId).Distinct().ToList();
        var adminIds = logs.Select(l => l.PerformedBy).Distinct().ToList();

        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var admins = await _dbContext.PlatformAdmins
            .AsNoTracking()
            .Where(a => adminIds.Contains(a.Id))
            .Select(a => new { a.Id, Name = a.FirstName + " " + a.LastName })
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        var dtos = logs.Select(l => new TenantActivityLogDto
        {
            Id = l.Id,
            TenantId = l.TenantId,
            TenantName = tenants.GetValueOrDefault(l.TenantId, "Unknown"),
            PerformedBy = l.PerformedBy,
            PerformedByName = admins.GetValueOrDefault(l.PerformedBy, "System"),
            Action = l.Action,
            Details = l.Details,
            IpAddress = l.IpAddress,
            PerformedAt = l.PerformedAt
        }).ToList();

        return new PaginatedResult<TenantActivityLogDto>(
            dtos,
            totalCount,
            request.Page,
            request.PageSize);
    }
}

/// <summary>
/// Query to get platform statistics.
/// </summary>
public class GetPlatformStatsQuery : IRequest<PlatformStatsDto>
{
}

public class GetPlatformStatsQueryHandler : IRequestHandler<GetPlatformStatsQuery, PlatformStatsDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPlatformStatsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformStatsDto> Handle(GetPlatformStatsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var activeStatuses = new[] { SubscriptionStatus.Active, SubscriptionStatus.Trial };
        var subscriptions = await _dbContext.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => activeStatuses.Contains(s.Status))
            .ToListAsync(cancellationToken);

        var stats = new PlatformStatsDto
        {
            TotalTenants = tenants.Count,
            ActiveTenants = tenants.Count(t => t.Status == TenantStatus.Active),
            TrialTenants = tenants.Count(t => t.IsTrialActive()),
            SuspendedTenants = tenants.Count(t => t.Status == TenantStatus.Suspended),
            TenantsThisMonth = tenants.Count(t => t.CreatedAt >= startOfMonth),
            TenantsByPlan = subscriptions
                .GroupBy(s => s.Plan?.Name ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }
}
