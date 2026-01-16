using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Infrastructure.Persistence;

namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that syncs inventory levels with sales channels.
/// </summary>
public class InventorySyncJob : IBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventorySyncJob> _logger;

    public InventorySyncJob(
        IServiceProvider serviceProvider,
        ILogger<InventorySyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        var jobArgs = args as InventorySyncJobArgs;

        _logger.LogDebug("Starting inventory sync job");

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

            var totalChannelsSynced = 0;

            // Process inventory sync for each tenant
            foreach (var tenant in tenants)
            {
                try
                {
                    var channelsSynced = await ProcessTenantInventorySyncAsync(
                        tenant.Id, tenant.SchemaName, tenant.Slug, jobArgs, cancellationToken);
                    totalChannelsSynced += channelsSynced;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to process inventory sync for tenant {TenantId}", tenant.Id);
                }
            }

            if (totalChannelsSynced > 0)
            {
                _logger.LogInformation("Inventory sync job completed. Synced {Count} channels", totalChannelsSynced);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory sync job failed");
            throw;
        }
    }

    private async Task<int> ProcessTenantInventorySyncAsync(
        Guid tenantId, string schemaName, string slug, InventorySyncJobArgs? jobArgs, CancellationToken cancellationToken)
    {
        // Create a new scope for this tenant
        using var tenantScope = _serviceProvider.CreateScope();

        // Set the tenant context
        var currentTenantService = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        currentTenantService.SetTenant(tenantId, schemaName, slug);

        var dbContext = tenantScope.ServiceProvider.GetRequiredService<ITenantDbContext>();

        // Get products with low stock that need syncing
        var lowStockProducts = await dbContext.Inventory
            .AsNoTracking()
            .Include(i => i.Product)
            .Where(i => i.QuantityOnHand - i.QuantityReserved <= i.ReorderPoint)
            .ToListAsync(cancellationToken);

        if (lowStockProducts.Count > 0)
        {
            _logger.LogDebug("{Count} products are at or below reorder point for tenant {TenantId}",
                lowStockProducts.Count, tenantId);

            // In production, this would:
            // 1. Send low stock alerts
            // 2. Sync inventory to connected sales channels
            // 3. Potentially trigger auto-reorder if configured
        }

        // Get active sales channels
        var activeChannels = await dbContext.SalesChannels
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        if (activeChannels.Count > 0)
        {
            _logger.LogDebug("Syncing inventory to {ChannelCount} active channels for tenant {TenantId}",
                activeChannels.Count, tenantId);
        }

        foreach (var channel in activeChannels)
        {
            try
            {
                await SyncChannelInventoryAsync(channel.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync inventory for channel {ChannelId}", channel.Id);
            }
        }

        return activeChannels.Count;
    }

    private Task SyncChannelInventoryAsync(Guid channelId, CancellationToken cancellationToken)
    {
        // In production, this would call the appropriate channel adapter
        // to push inventory updates to the sales channel
        _logger.LogDebug("Would sync inventory for channel {ChannelId}", channelId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Arguments for inventory sync job.
/// </summary>
public class InventorySyncJobArgs
{
    public Guid? ChannelId { get; set; }
    public Guid? ProductId { get; set; }
    public bool ForceSync { get; set; }
}

/// <summary>
/// Configuration for inventory sync service.
/// </summary>
public class InventorySyncSettings
{
    public const string SectionName = "InventorySync";

    /// <summary>
    /// Whether automatic sync is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between sync runs in minutes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Whether to send low stock alerts.
    /// </summary>
    public bool SendLowStockAlerts { get; set; } = true;
}

/// <summary>
/// Background service that periodically syncs inventory.
/// </summary>
public class InventorySyncHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventorySyncHostedService> _logger;
    private readonly InventorySyncSettings _settings;

    public InventorySyncHostedService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<InventorySyncSettings> settings,
        ILogger<InventorySyncHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Inventory sync service is disabled");
            return;
        }

        _logger.LogInformation(
            "Inventory sync service started. Interval: {Interval} minutes",
            _settings.IntervalMinutes);

        // Wait before first run
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<InventorySyncJob>();
                await job.ExecuteAsync(null, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in inventory sync cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Inventory sync service stopped");
    }
}
