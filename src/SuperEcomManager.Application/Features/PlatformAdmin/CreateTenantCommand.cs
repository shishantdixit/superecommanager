using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Platform;
using SuperEcomManager.Domain.Entities.Subscriptions;
using SuperEcomManager.Domain.Entities.Tenants;

namespace SuperEcomManager.Application.Features.PlatformAdmin;

/// <summary>
/// Command to create a new tenant (platform admin only).
/// </summary>
public class CreateTenantCommand : IRequest<Result<TenantAdminDto>>
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? GstNumber { get; set; }
    public string PlanCode { get; set; } = "free";
    public int TrialDays { get; set; } = 14;

    // Owner account details
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerPassword { get; set; } = string.Empty;
    public string? OwnerFirstName { get; set; }
    public string? OwnerLastName { get; set; }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<TenantAdminDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantSeeder _tenantSeeder;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        IApplicationDbContext dbContext,
        ITenantSeeder tenantSeeder,
        ICurrentUserService currentUserService,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _dbContext = dbContext;
        _tenantSeeder = tenantSeeder;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<TenantAdminDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        // Validate slug uniqueness
        var slugExists = await _dbContext.Tenants
            .AnyAsync(t => t.Slug == request.Slug.ToLowerInvariant(), cancellationToken);

        if (slugExists)
        {
            return Result<TenantAdminDto>.Failure($"Tenant with slug '{request.Slug}' already exists");
        }

        // Validate plan exists
        var plan = await _dbContext.Plans
            .FirstOrDefaultAsync(p => p.Code == request.PlanCode.ToLowerInvariant(), cancellationToken);

        if (plan == null)
        {
            return Result<TenantAdminDto>.Failure($"Plan '{request.PlanCode}' not found");
        }

        // Create tenant
        var tenant = Tenant.Create(
            request.Name,
            request.Slug,
            request.ContactEmail,
            request.TrialDays);

        tenant.UpdateProfile(
            request.CompanyName,
            null, // logoUrl
            null, // website
            request.ContactEmail,
            request.ContactPhone,
            request.Address,
            request.GstNumber);

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Create subscription with trial period
        var subscription = Subscription.CreateTrial(tenant.Id, plan.Id, request.TrialDays);
        _dbContext.Subscriptions.Add(subscription);

        // Log activity
        var activityLog = TenantActivityLog.Create(
            tenant.Id,
            _currentUserService.UserId ?? Guid.Empty,
            TenantActivityActions.Created,
            $"Tenant created with plan: {plan.Name}");
        _dbContext.TenantActivityLogs.Add(activityLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Initialize tenant schema with owner user
        try
        {
            await _tenantSeeder.InitializeTenantAsync(
                tenant.Id,
                tenant.SchemaName,
                request.OwnerEmail,
                request.OwnerPassword,
                request.CompanyName ?? request.Name,
                cancellationToken);

            // Activate tenant after successful initialization
            tenant.Activate();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created tenant {TenantId} with schema {Schema}", tenant.Id, tenant.SchemaName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tenant schema for {TenantId}", tenant.Id);
            return Result<TenantAdminDto>.Failure($"Failed to initialize tenant: {ex.Message}");
        }

        return Result<TenantAdminDto>.Success(new TenantAdminDto
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
            CurrentPlan = plan.Name,
            CreatedAt = tenant.CreatedAt
        });
    }
}
