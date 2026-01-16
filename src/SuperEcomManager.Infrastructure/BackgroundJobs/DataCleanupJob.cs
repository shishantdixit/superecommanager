using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Infrastructure.Persistence;

namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that cleans up old data to maintain database performance.
/// </summary>
public class DataCleanupJob : IBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataCleanupJob> _logger;

    public DataCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<DataCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        var settings = args as DataCleanupJobArgs ?? new DataCleanupJobArgs();

        _logger.LogDebug("Starting data cleanup job");

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

            var grandTotalDeleted = 0;

            // Process cleanup for each tenant
            foreach (var tenant in tenants)
            {
                try
                {
                    var deleted = await ProcessTenantCleanupAsync(
                        tenant.Id, tenant.SchemaName, tenant.Slug, settings, cancellationToken);
                    grandTotalDeleted += deleted;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to process cleanup for tenant {TenantId}", tenant.Id);
                }
            }

            if (grandTotalDeleted > 0)
            {
                _logger.LogInformation("Data cleanup job completed. Total records deleted: {Total}", grandTotalDeleted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data cleanup job failed");
            throw;
        }
    }

    private async Task<int> ProcessTenantCleanupAsync(
        Guid tenantId, string schemaName, string slug, DataCleanupJobArgs settings, CancellationToken cancellationToken)
    {
        // Create a new scope for this tenant
        using var tenantScope = _serviceProvider.CreateScope();

        // Set the tenant context
        var currentTenantService = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        currentTenantService.SetTenant(tenantId, schemaName, slug);

        var dbContext = tenantScope.ServiceProvider.GetRequiredService<ITenantDbContext>();

        var totalDeleted = 0;

        // Clean up old audit logs
        if (settings.CleanAuditLogs)
        {
            var auditCutoff = DateTime.UtcNow.AddDays(-settings.AuditLogRetentionDays);
            var auditLogsDeleted = await CleanupAuditLogsAsync(dbContext, auditCutoff, cancellationToken);
            totalDeleted += auditLogsDeleted;
            if (auditLogsDeleted > 0)
                _logger.LogDebug("Deleted {Count} old audit logs for tenant {TenantId}", auditLogsDeleted, tenantId);
        }

        // Clean up old webhook deliveries
        if (settings.CleanWebhookDeliveries)
        {
            var webhookCutoff = DateTime.UtcNow.AddDays(-settings.WebhookDeliveryRetentionDays);
            var webhooksDeleted = await CleanupWebhookDeliveriesAsync(dbContext, webhookCutoff, cancellationToken);
            totalDeleted += webhooksDeleted;
            if (webhooksDeleted > 0)
                _logger.LogDebug("Deleted {Count} old webhook deliveries for tenant {TenantId}", webhooksDeleted, tenantId);
        }

        // Clean up old notification logs
        if (settings.CleanNotificationLogs)
        {
            var notificationCutoff = DateTime.UtcNow.AddDays(-settings.NotificationLogRetentionDays);
            var notificationsDeleted = await CleanupNotificationLogsAsync(dbContext, notificationCutoff, cancellationToken);
            totalDeleted += notificationsDeleted;
            if (notificationsDeleted > 0)
                _logger.LogDebug("Deleted {Count} old notification logs for tenant {TenantId}", notificationsDeleted, tenantId);
        }

        // Clean up expired refresh tokens
        if (settings.CleanExpiredTokens)
        {
            var tokensDeleted = await CleanupExpiredTokensAsync(dbContext, cancellationToken);
            totalDeleted += tokensDeleted;
            if (tokensDeleted > 0)
                _logger.LogDebug("Deleted {Count} expired refresh tokens for tenant {TenantId}", tokensDeleted, tenantId);
        }

        return totalDeleted;
    }

    private async Task<int> CleanupAuditLogsAsync(
        ITenantDbContext dbContext,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        var oldLogs = await dbContext.AuditLogs
            .Where(a => a.Timestamp < cutoffDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        if (oldLogs.Count > 0)
        {
            dbContext.AuditLogs.RemoveRange(oldLogs);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return oldLogs.Count;
    }

    private async Task<int> CleanupWebhookDeliveriesAsync(
        ITenantDbContext dbContext,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        var oldDeliveries = await dbContext.WebhookDeliveries
            .Where(d => d.CreatedAt < cutoffDate &&
                       (d.Status == WebhookDeliveryStatus.Delivered ||
                        d.Status == WebhookDeliveryStatus.Failed))
            .Take(1000)
            .ToListAsync(cancellationToken);

        if (oldDeliveries.Count > 0)
        {
            dbContext.WebhookDeliveries.RemoveRange(oldDeliveries);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return oldDeliveries.Count;
    }

    private async Task<int> CleanupNotificationLogsAsync(
        ITenantDbContext dbContext,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        var oldNotifications = await dbContext.NotificationLogs
            .Where(n => n.CreatedAt < cutoffDate &&
                       (n.Status == "Sent" || n.Status == "Delivered" || n.Status == "Failed"))
            .Take(1000)
            .ToListAsync(cancellationToken);

        if (oldNotifications.Count > 0)
        {
            dbContext.NotificationLogs.RemoveRange(oldNotifications);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return oldNotifications.Count;
    }

    private async Task<int> CleanupExpiredTokensAsync(
        ITenantDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var expiredTokens = await dbContext.RefreshTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow || t.RevokedAt.HasValue)
            .Take(500)
            .ToListAsync(cancellationToken);

        if (expiredTokens.Count > 0)
        {
            dbContext.RefreshTokens.RemoveRange(expiredTokens);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return expiredTokens.Count;
    }
}

/// <summary>
/// Arguments for data cleanup job.
/// </summary>
public class DataCleanupJobArgs
{
    public bool CleanAuditLogs { get; set; } = true;
    public bool CleanWebhookDeliveries { get; set; } = true;
    public bool CleanNotificationLogs { get; set; } = true;
    public bool CleanExpiredTokens { get; set; } = true;
    public int AuditLogRetentionDays { get; set; } = 90;
    public int WebhookDeliveryRetentionDays { get; set; } = 30;
    public int NotificationLogRetentionDays { get; set; } = 30;
}

/// <summary>
/// Configuration for data cleanup service.
/// </summary>
public class DataCleanupSettings
{
    public const string SectionName = "DataCleanup";

    /// <summary>
    /// Whether automatic cleanup is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Hour of day (0-23) to run cleanup. Default: 2 AM.
    /// </summary>
    public int RunAtHour { get; set; } = 2;

    public bool CleanAuditLogs { get; set; } = true;
    public bool CleanWebhookDeliveries { get; set; } = true;
    public bool CleanNotificationLogs { get; set; } = true;
    public bool CleanExpiredTokens { get; set; } = true;
    public int AuditLogRetentionDays { get; set; } = 90;
    public int WebhookDeliveryRetentionDays { get; set; } = 30;
    public int NotificationLogRetentionDays { get; set; } = 30;
}

/// <summary>
/// Background service that periodically cleans up old data.
/// </summary>
public class DataCleanupHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataCleanupHostedService> _logger;
    private readonly DataCleanupSettings _settings;

    public DataCleanupHostedService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<DataCleanupSettings> settings,
        ILogger<DataCleanupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Data cleanup service is disabled");
            return;
        }

        _logger.LogInformation(
            "Data cleanup service started. Runs daily at {Hour}:00",
            _settings.RunAtHour);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, _settings.RunAtHour, 0, 0);

            if (nextRun <= now)
                nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            _logger.LogDebug("Next data cleanup scheduled for {NextRun}", nextRun);

            try
            {
                await Task.Delay(delay, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<DataCleanupJob>();

                await job.ExecuteAsync(new DataCleanupJobArgs
                {
                    CleanAuditLogs = _settings.CleanAuditLogs,
                    CleanWebhookDeliveries = _settings.CleanWebhookDeliveries,
                    CleanNotificationLogs = _settings.CleanNotificationLogs,
                    CleanExpiredTokens = _settings.CleanExpiredTokens,
                    AuditLogRetentionDays = _settings.AuditLogRetentionDays,
                    WebhookDeliveryRetentionDays = _settings.WebhookDeliveryRetentionDays,
                    NotificationLogRetentionDays = _settings.NotificationLogRetentionDays
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data cleanup cycle");
            }
        }

        _logger.LogInformation("Data cleanup service stopped");
    }
}
