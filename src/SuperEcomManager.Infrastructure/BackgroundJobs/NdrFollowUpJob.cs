using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Infrastructure.Persistence;

namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that processes NDR cases requiring follow-up action.
/// </summary>
public class NdrFollowUpJob : IBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NdrFollowUpJob> _logger;

    public NdrFollowUpJob(
        IServiceProvider serviceProvider,
        ILogger<NdrFollowUpJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        var settings = args as NdrFollowUpJobArgs ?? new NdrFollowUpJobArgs();

        _logger.LogDebug("Starting NDR follow-up job");

        try
        {
            // Get all active tenants from shared database
            using var scope = _serviceProvider.CreateScope();
            var appDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var tenants = await appDbContext.Tenants
                .AsNoTracking()
                .Where(t => t.Status == Domain.Enums.TenantStatus.Active)
                .Select(t => new { t.Id, t.SchemaName, t.Slug })
                .ToListAsync(cancellationToken);

            if (tenants.Count == 0)
            {
                _logger.LogDebug("No active tenants found");
                return;
            }

            var totalProcessed = 0;

            // Process NDR follow-ups for each tenant
            foreach (var tenant in tenants)
            {
                try
                {
                    var processed = await ProcessTenantNdrFollowUpsAsync(
                        tenant.Id, tenant.SchemaName, tenant.Slug, settings, cancellationToken);
                    totalProcessed += processed;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to process NDR follow-ups for tenant {TenantId}", tenant.Id);
                }
            }

            if (totalProcessed > 0)
            {
                _logger.LogInformation("NDR follow-up job completed. Processed {Count} NDR cases", totalProcessed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NDR follow-up job failed");
            throw;
        }
    }

    private async Task<int> ProcessTenantNdrFollowUpsAsync(
        Guid tenantId, string schemaName, string slug, NdrFollowUpJobArgs settings, CancellationToken cancellationToken)
    {
        // Create a new scope for this tenant
        using var tenantScope = _serviceProvider.CreateScope();

        // Set the tenant context
        var currentTenantService = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        currentTenantService.SetTenant(tenantId, schemaName, slug);

        var dbContext = tenantScope.ServiceProvider.GetRequiredService<ITenantDbContext>();
        var webhookDispatcher = tenantScope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

        var processedCount = 0;

        // Process overdue follow-ups
        var overdueNdrs = await GetOverdueNdrsAsync(dbContext, cancellationToken);
        foreach (var ndr in overdueNdrs)
        {
            await ProcessOverdueNdrAsync(dbContext, webhookDispatcher, ndr, cancellationToken);
            processedCount++;
        }

        // Process unassigned NDRs that are aging
        var unassignedNdrs = await GetAgingUnassignedNdrsAsync(dbContext, settings.UnassignedAlertHours, cancellationToken);
        foreach (var ndr in unassignedNdrs)
        {
            await EscalateUnassignedNdrAsync(dbContext, webhookDispatcher, ndr, cancellationToken);
            processedCount++;
        }

        if (processedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Processed {Count} NDR cases for tenant {TenantId}", processedCount, tenantId);
        }

        return processedCount;
    }

    private async Task<List<NdrFollowUpItem>> GetOverdueNdrsAsync(
        ITenantDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.NdrRecords
            .AsNoTracking()
            .Where(n => n.Status == NdrStatus.ReattemptScheduled &&
                       n.NextFollowUpAt.HasValue &&
                       n.NextFollowUpAt <= DateTime.UtcNow)
            .Select(n => new NdrFollowUpItem
            {
                Id = n.Id,
                OrderId = n.OrderId,
                ShipmentId = n.ShipmentId,
                AwbNumber = n.AwbNumber,
                Status = n.Status,
                ReasonCode = n.ReasonCode,
                AttemptCount = n.AttemptCount,
                AssignedToUserId = n.AssignedToUserId,
                NextFollowUpAt = n.NextFollowUpAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<NdrFollowUpItem>> GetAgingUnassignedNdrsAsync(
        ITenantDbContext dbContext,
        int hoursThreshold,
        CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddHours(-hoursThreshold);

        return await dbContext.NdrRecords
            .AsNoTracking()
            .Where(n => n.Status == NdrStatus.Open &&
                       n.AssignedToUserId == null &&
                       n.CreatedAt < cutoffDate)
            .Select(n => new NdrFollowUpItem
            {
                Id = n.Id,
                OrderId = n.OrderId,
                ShipmentId = n.ShipmentId,
                AwbNumber = n.AwbNumber,
                Status = n.Status,
                ReasonCode = n.ReasonCode,
                AttemptCount = n.AttemptCount,
                AssignedToUserId = n.AssignedToUserId,
                NextFollowUpAt = n.NextFollowUpAt
            })
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    private async Task ProcessOverdueNdrAsync(
        ITenantDbContext dbContext,
        IWebhookDispatcher webhookDispatcher,
        NdrFollowUpItem ndr,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing overdue NDR {NdrId} for AWB {AwbNumber}", ndr.Id, ndr.AwbNumber);

        // Dispatch webhook notification for overdue follow-up
        await webhookDispatcher.DispatchAsync(
            WebhookEvent.NdrEscalated,
            new
            {
                NdrId = ndr.Id,
                OrderId = ndr.OrderId,
                ShipmentId = ndr.ShipmentId,
                AwbNumber = ndr.AwbNumber,
                Reason = "Follow-up overdue",
                AttemptCount = ndr.AttemptCount,
                ScheduledFollowUp = ndr.NextFollowUpAt,
                AssignedToUserId = ndr.AssignedToUserId
            },
            cancellationToken);
    }

    private async Task EscalateUnassignedNdrAsync(
        ITenantDbContext dbContext,
        IWebhookDispatcher webhookDispatcher,
        NdrFollowUpItem ndr,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Escalating unassigned NDR {NdrId} for AWB {AwbNumber}", ndr.Id, ndr.AwbNumber);

        // Dispatch webhook notification for unassigned NDR
        await webhookDispatcher.DispatchAsync(
            WebhookEvent.NdrEscalated,
            new
            {
                NdrId = ndr.Id,
                OrderId = ndr.OrderId,
                ShipmentId = ndr.ShipmentId,
                AwbNumber = ndr.AwbNumber,
                Reason = "Unassigned NDR aging",
                ReasonCode = ndr.ReasonCode.ToString(),
                AttemptCount = ndr.AttemptCount
            },
            cancellationToken);
    }
}

/// <summary>
/// Internal DTO for NDR follow-up processing.
/// </summary>
internal class NdrFollowUpItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ShipmentId { get; set; }
    public string AwbNumber { get; set; } = string.Empty;
    public NdrStatus Status { get; set; }
    public NdrReasonCode ReasonCode { get; set; }
    public int AttemptCount { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
}

/// <summary>
/// Arguments for NDR follow-up job.
/// </summary>
public class NdrFollowUpJobArgs
{
    public int UnassignedAlertHours { get; set; } = 4;
}

/// <summary>
/// Configuration for NDR follow-up service.
/// </summary>
public class NdrFollowUpSettings
{
    public const string SectionName = "NdrFollowUp";

    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 30;
    public int UnassignedAlertHours { get; set; } = 4;
}

/// <summary>
/// Background service that periodically processes NDR follow-ups.
/// </summary>
public class NdrFollowUpHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NdrFollowUpHostedService> _logger;
    private readonly NdrFollowUpSettings _settings;

    public NdrFollowUpHostedService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<NdrFollowUpSettings> settings,
        ILogger<NdrFollowUpHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("NDR follow-up service is disabled");
            return;
        }

        _logger.LogInformation(
            "NDR follow-up service started. Interval: {Interval} minutes",
            _settings.IntervalMinutes);

        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<NdrFollowUpJob>();

                await job.ExecuteAsync(new NdrFollowUpJobArgs
                {
                    UnassignedAlertHours = _settings.UnassignedAlertHours
                }, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in NDR follow-up cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("NDR follow-up service stopped");
    }
}
