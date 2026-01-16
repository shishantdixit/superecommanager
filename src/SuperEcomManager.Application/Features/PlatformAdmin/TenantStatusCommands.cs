using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Platform;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.PlatformAdmin;

/// <summary>
/// Command to suspend a tenant.
/// </summary>
public class SuspendTenantCommand : IRequest<Result>
{
    public Guid TenantId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class SuspendTenantCommandHandler : IRequestHandler<SuspendTenantCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SuspendTenantCommandHandler> _logger;

    public SuspendTenantCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<SuspendTenantCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            return Result.Failure("Tenant not found");
        }

        if (tenant.Status == TenantStatus.Suspended)
        {
            return Result.Failure("Tenant is already suspended");
        }

        if (tenant.Status == TenantStatus.Deactivated)
        {
            return Result.Failure("Cannot suspend a deactivated tenant");
        }

        tenant.Suspend(request.Reason);

        var activityLog = TenantActivityLog.Create(
            tenant.Id,
            _currentUserService.UserId ?? Guid.Empty,
            TenantActivityActions.Suspended,
            $"Reason: {request.Reason}");
        _dbContext.TenantActivityLogs.Add(activityLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Suspended tenant {TenantId}. Reason: {Reason}", tenant.Id, request.Reason);

        return Result.Success();
    }
}

/// <summary>
/// Command to reactivate a suspended tenant.
/// </summary>
public class ReactivateTenantCommand : IRequest<Result>
{
    public Guid TenantId { get; set; }
    public string? Notes { get; set; }
}

public class ReactivateTenantCommandHandler : IRequestHandler<ReactivateTenantCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ReactivateTenantCommandHandler> _logger;

    public ReactivateTenantCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<ReactivateTenantCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(ReactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            return Result.Failure("Tenant not found");
        }

        if (tenant.Status != TenantStatus.Suspended)
        {
            return Result.Failure("Only suspended tenants can be reactivated");
        }

        tenant.Activate();

        var activityLog = TenantActivityLog.Create(
            tenant.Id,
            _currentUserService.UserId ?? Guid.Empty,
            TenantActivityActions.Reactivated,
            request.Notes);
        _dbContext.TenantActivityLogs.Add(activityLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reactivated tenant {TenantId}", tenant.Id);

        return Result.Success();
    }
}

/// <summary>
/// Command to deactivate a tenant permanently.
/// </summary>
public class DeactivateTenantCommand : IRequest<Result>
{
    public Guid TenantId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool DeleteData { get; set; } = false;
}

public class DeactivateTenantCommandHandler : IRequestHandler<DeactivateTenantCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeactivateTenantCommandHandler> _logger;

    public DeactivateTenantCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<DeactivateTenantCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            return Result.Failure("Tenant not found");
        }

        if (tenant.Status == TenantStatus.Deactivated)
        {
            return Result.Failure("Tenant is already deactivated");
        }

        tenant.Deactivate();

        var details = $"Reason: {request.Reason}. Data deletion: {(request.DeleteData ? "Scheduled" : "Retained")}";
        var activityLog = TenantActivityLog.Create(
            tenant.Id,
            _currentUserService.UserId ?? Guid.Empty,
            TenantActivityActions.Deactivated,
            details);
        _dbContext.TenantActivityLogs.Add(activityLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Deactivated tenant {TenantId}. Reason: {Reason}", tenant.Id, request.Reason);

        // TODO: If DeleteData is true, schedule tenant data deletion job

        return Result.Success();
    }
}

/// <summary>
/// Command to extend trial period.
/// </summary>
public class ExtendTrialCommand : IRequest<Result>
{
    public Guid TenantId { get; set; }
    public int AdditionalDays { get; set; }
    public string? Reason { get; set; }
}

public class ExtendTrialCommandHandler : IRequestHandler<ExtendTrialCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ExtendTrialCommandHandler> _logger;

    public ExtendTrialCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<ExtendTrialCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(ExtendTrialCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            return Result.Failure("Tenant not found");
        }

        // Use reflection to set TrialEndsAt since it's private
        var newTrialEnd = (tenant.TrialEndsAt ?? DateTime.UtcNow).AddDays(request.AdditionalDays);
        var property = typeof(Domain.Entities.Tenants.Tenant).GetProperty("TrialEndsAt");
        property?.SetValue(tenant, newTrialEnd);

        var activityLog = TenantActivityLog.Create(
            tenant.Id,
            _currentUserService.UserId ?? Guid.Empty,
            TenantActivityActions.TrialExtended,
            $"Extended by {request.AdditionalDays} days. New end: {newTrialEnd:yyyy-MM-dd}. Reason: {request.Reason}");
        _dbContext.TenantActivityLogs.Add(activityLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Extended trial for tenant {TenantId} by {Days} days",
            tenant.Id, request.AdditionalDays);

        return Result.Success();
    }
}
