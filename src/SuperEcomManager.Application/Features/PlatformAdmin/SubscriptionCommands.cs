using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Platform;
using SuperEcomManager.Domain.Entities.Subscriptions;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.PlatformAdmin;

#region DTOs

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public bool IsYearly { get; set; }
    public decimal PriceAtSubscription { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

#endregion

#region Commands

/// <summary>
/// Command to change a tenant's subscription plan.
/// </summary>
public class ChangeTenantPlanCommand : IRequest<Result<SubscriptionDto>>
{
    public Guid TenantId { get; set; }
    public Guid NewPlanId { get; set; }
    public bool IsYearly { get; set; }
    public string? Notes { get; set; }
}

public class ChangeTenantPlanCommandHandler : IRequestHandler<ChangeTenantPlanCommand, Result<SubscriptionDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChangeTenantPlanCommandHandler> _logger;

    public ChangeTenantPlanCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<ChangeTenantPlanCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<SubscriptionDto>> Handle(ChangeTenantPlanCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant == null)
        {
            return Result<SubscriptionDto>.Failure("Tenant not found");
        }

        var newPlan = await _dbContext.Plans
            .FirstOrDefaultAsync(p => p.Id == request.NewPlanId && p.IsActive, cancellationToken);

        if (newPlan == null)
        {
            return Result<SubscriptionDto>.Failure("Plan not found or inactive");
        }

        var activeStatuses = new[] { SubscriptionStatus.Active, SubscriptionStatus.Trial };
        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId && activeStatuses.Contains(s.Status), cancellationToken);

        if (subscription == null)
        {
            // Create new subscription if none exists
            subscription = Subscription.CreateTrial(request.TenantId, request.NewPlanId, 0);
            subscription.Activate(
                request.IsYearly ? newPlan.YearlyPrice : newPlan.MonthlyPrice,
                request.IsYearly);
            _dbContext.Subscriptions.Add(subscription);
        }
        else
        {
            var price = request.IsYearly ? newPlan.YearlyPrice : newPlan.MonthlyPrice;
            subscription.ChangePlan(request.NewPlanId, price, request.IsYearly);
        }

        // Log activity
        var activityLog = TenantActivityLog.Create(
            tenant.Id,
            _currentUserService.UserId ?? Guid.Empty,
            TenantActivityActions.PlanChanged,
            $"Changed to plan: {newPlan.Name}. {request.Notes}");
        _dbContext.TenantActivityLogs.Add(activityLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Changed tenant {TenantId} to plan {PlanCode}", tenant.Id, newPlan.Code);

        return Result<SubscriptionDto>.Success(new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = tenant.Name,
            PlanId = newPlan.Id,
            PlanName = newPlan.Name,
            PlanCode = newPlan.Code,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndsAt = subscription.TrialEndsAt,
            IsYearly = subscription.IsYearly,
            PriceAtSubscription = subscription.PriceAtSubscription,
            Currency = subscription.Currency,
            CreatedAt = subscription.CreatedAt
        });
    }
}

/// <summary>
/// Command to activate a subscription (convert trial to paid).
/// </summary>
public class ActivateSubscriptionCommand : IRequest<Result<SubscriptionDto>>
{
    public Guid SubscriptionId { get; set; }
    public bool IsYearly { get; set; }
    public decimal? OverridePrice { get; set; }
}

public class ActivateSubscriptionCommandHandler : IRequestHandler<ActivateSubscriptionCommand, Result<SubscriptionDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ActivateSubscriptionCommandHandler> _logger;

    public ActivateSubscriptionCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<ActivateSubscriptionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<SubscriptionDto>> Handle(ActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);

        if (subscription == null)
        {
            return Result<SubscriptionDto>.Failure("Subscription not found");
        }

        if (subscription.Plan == null)
        {
            return Result<SubscriptionDto>.Failure("Plan not found");
        }

        var price = request.OverridePrice ??
            (request.IsYearly ? subscription.Plan.YearlyPrice : subscription.Plan.MonthlyPrice);

        subscription.Activate(price, request.IsYearly);

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == subscription.TenantId, cancellationToken);

        var activityLog = TenantActivityLog.Create(
            subscription.TenantId,
            _currentUserService.UserId ?? Guid.Empty,
            TenantActivityActions.SubscriptionActivated,
            $"Subscription activated. Plan: {subscription.Plan.Name}, Price: {price:C}");
        _dbContext.TenantActivityLogs.Add(activityLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated subscription {SubscriptionId}", subscription.Id);

        return Result<SubscriptionDto>.Success(new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = tenant?.Name ?? "Unknown",
            PlanId = subscription.Plan.Id,
            PlanName = subscription.Plan.Name,
            PlanCode = subscription.Plan.Code,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndsAt = subscription.TrialEndsAt,
            IsYearly = subscription.IsYearly,
            PriceAtSubscription = subscription.PriceAtSubscription,
            Currency = subscription.Currency,
            CreatedAt = subscription.CreatedAt
        });
    }
}

/// <summary>
/// Command to cancel a subscription.
/// </summary>
public class CancelSubscriptionCommand : IRequest<Result>
{
    public Guid SubscriptionId { get; set; }
    public string? Reason { get; set; }
    public bool ImmediateCancel { get; set; }
}

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<CancelSubscriptionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);

        if (subscription == null)
        {
            return Result.Failure("Subscription not found");
        }

        subscription.Cancel(request.Reason);

        var activityLog = TenantActivityLog.Create(
            subscription.TenantId,
            _currentUserService.UserId ?? Guid.Empty,
            TenantActivityActions.SubscriptionCancelled,
            $"Subscription cancelled. Reason: {request.Reason}");
        _dbContext.TenantActivityLogs.Add(activityLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscription.Id);

        return Result.Success();
    }
}

/// <summary>
/// Command to renew a subscription.
/// </summary>
public class RenewSubscriptionCommand : IRequest<Result<SubscriptionDto>>
{
    public Guid SubscriptionId { get; set; }
    public bool IsYearly { get; set; }
    public decimal? OverridePrice { get; set; }
}

public class RenewSubscriptionCommandHandler : IRequestHandler<RenewSubscriptionCommand, Result<SubscriptionDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RenewSubscriptionCommandHandler> _logger;

    public RenewSubscriptionCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<RenewSubscriptionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<SubscriptionDto>> Handle(RenewSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);

        if (subscription == null)
        {
            return Result<SubscriptionDto>.Failure("Subscription not found");
        }

        if (subscription.Plan == null)
        {
            return Result<SubscriptionDto>.Failure("Plan not found");
        }

        var price = request.OverridePrice ??
            (request.IsYearly ? subscription.Plan.YearlyPrice : subscription.Plan.MonthlyPrice);

        subscription.Renew(price, request.IsYearly);

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == subscription.TenantId, cancellationToken);

        var activityLog = TenantActivityLog.Create(
            subscription.TenantId,
            _currentUserService.UserId ?? Guid.Empty,
            TenantActivityActions.SubscriptionRenewed,
            $"Subscription renewed. Plan: {subscription.Plan.Name}, Price: {price:C}");
        _dbContext.TenantActivityLogs.Add(activityLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Renewed subscription {SubscriptionId}", subscription.Id);

        return Result<SubscriptionDto>.Success(new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = tenant?.Name ?? "Unknown",
            PlanId = subscription.Plan.Id,
            PlanName = subscription.Plan.Name,
            PlanCode = subscription.Plan.Code,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            IsYearly = subscription.IsYearly,
            PriceAtSubscription = subscription.PriceAtSubscription,
            Currency = subscription.Currency,
            CreatedAt = subscription.CreatedAt
        });
    }
}

#endregion

#region Queries

/// <summary>
/// Query to get subscriptions with filtering.
/// </summary>
public class GetSubscriptionsQuery : IRequest<PaginatedResult<SubscriptionDto>>
{
    public Guid? TenantId { get; set; }
    public Guid? PlanId { get; set; }
    public SubscriptionStatus? Status { get; set; }
    public bool? IsExpiringSoon { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetSubscriptionsQueryHandler : IRequestHandler<GetSubscriptionsQuery, PaginatedResult<SubscriptionDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetSubscriptionsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResult<SubscriptionDto>> Handle(GetSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .AsQueryable();

        if (request.TenantId.HasValue)
        {
            query = query.Where(s => s.TenantId == request.TenantId.Value);
        }

        if (request.PlanId.HasValue)
        {
            query = query.Where(s => s.PlanId == request.PlanId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(s => s.Status == request.Status.Value);
        }

        if (request.IsExpiringSoon == true)
        {
            var soonDate = DateTime.UtcNow.AddDays(7);
            query = query.Where(s => s.EndDate.HasValue && s.EndDate.Value <= soonDate && s.EndDate.Value > DateTime.UtcNow);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var subscriptions = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var tenantIds = subscriptions.Select(s => s.TenantId).Distinct().ToList();
        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var dtos = subscriptions.Select(s => new SubscriptionDto
        {
            Id = s.Id,
            TenantId = s.TenantId,
            TenantName = tenants.GetValueOrDefault(s.TenantId, "Unknown"),
            PlanId = s.PlanId,
            PlanName = s.Plan?.Name ?? "Unknown",
            PlanCode = s.Plan?.Code ?? "unknown",
            Status = s.Status,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            TrialEndsAt = s.TrialEndsAt,
            IsYearly = s.IsYearly,
            PriceAtSubscription = s.PriceAtSubscription,
            Currency = s.Currency,
            CancelledAt = s.CancelledAt,
            CancellationReason = s.CancellationReason,
            CreatedAt = s.CreatedAt
        }).ToList();

        return new PaginatedResult<SubscriptionDto>(
            dtos,
            totalCount,
            request.Page,
            request.PageSize);
    }
}

/// <summary>
/// Query to get subscription by ID.
/// </summary>
public class GetSubscriptionByIdQuery : IRequest<Result<SubscriptionDto>>
{
    public Guid SubscriptionId { get; set; }
}

public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, Result<SubscriptionDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetSubscriptionByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<SubscriptionDto>> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);

        if (subscription == null)
        {
            return Result<SubscriptionDto>.Failure("Subscription not found");
        }

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == subscription.TenantId, cancellationToken);

        return Result<SubscriptionDto>.Success(new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = tenant?.Name ?? "Unknown",
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.Name ?? "Unknown",
            PlanCode = subscription.Plan?.Code ?? "unknown",
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndsAt = subscription.TrialEndsAt,
            IsYearly = subscription.IsYearly,
            PriceAtSubscription = subscription.PriceAtSubscription,
            Currency = subscription.Currency,
            CancelledAt = subscription.CancelledAt,
            CancellationReason = subscription.CancellationReason,
            CreatedAt = subscription.CreatedAt
        });
    }
}

#endregion
