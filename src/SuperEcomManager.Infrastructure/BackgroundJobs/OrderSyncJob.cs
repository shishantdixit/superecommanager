using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that synchronizes orders from all active sales channels.
/// </summary>
public class OrderSyncJob : IBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderSyncJob> _logger;

    public OrderSyncJob(
        IServiceProvider serviceProvider,
        ILogger<OrderSyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        var jobArgs = args as ChannelSyncJobArgs;

        _logger.LogInformation("Starting order sync job");

        try
        {
            using var scope = _serviceProvider.CreateScope();

            // If specific channel/tenant is provided, sync only that
            if (jobArgs?.ChannelId.HasValue == true && jobArgs?.TenantId.HasValue == true)
            {
                await SyncChannelOrdersAsync(
                    scope.ServiceProvider,
                    jobArgs.TenantId.Value,
                    jobArgs.ChannelId.Value,
                    jobArgs.FromDate,
                    jobArgs.ToDate,
                    cancellationToken);
            }
            else
            {
                // Sync all active channels across all tenants
                await SyncAllChannelsAsync(scope.ServiceProvider, cancellationToken);
            }

            _logger.LogInformation("Order sync job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order sync job failed");
            throw;
        }
    }

    private async Task SyncAllChannelsAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // Get all active Shopify channels across all tenants
        // This requires access to the main database to get tenant info
        // then iterating through each tenant's schema

        // For now, log that we need tenant context
        _logger.LogInformation("Sync all channels - requires tenant iteration (not implemented in base job)");

        // In production, you would:
        // 1. Get list of all tenants from main DB
        // 2. For each tenant, set tenant context
        // 3. Get active Shopify channels
        // 4. Sync each channel

        await Task.CompletedTask;
    }

    private async Task SyncChannelOrdersAsync(
        IServiceProvider serviceProvider,
        Guid tenantId,
        Guid channelId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Syncing orders for channel {ChannelId} in tenant {TenantId}",
            channelId, tenantId);

        // This would be implemented with proper tenant context setup
        // For now, we log the intent
        _logger.LogInformation(
            "Would sync channel {ChannelId} from {FromDate} to {ToDate}",
            channelId, fromDate, toDate);

        await Task.CompletedTask;
    }
}
