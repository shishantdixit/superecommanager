using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that updates shipment tracking status from courier APIs.
/// </summary>
public class ShipmentTrackingUpdateJob : IBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ShipmentTrackingUpdateJob> _logger;

    public ShipmentTrackingUpdateJob(
        IServiceProvider serviceProvider,
        ILogger<ShipmentTrackingUpdateJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        var settings = args as ShipmentTrackingJobArgs ?? new ShipmentTrackingJobArgs();

        _logger.LogInformation("Starting shipment tracking update job");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITenantDbContext>();
            var webhookDispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

            // Get shipments that need tracking updates
            var shipmentsToUpdate = await GetShipmentsForTrackingAsync(
                dbContext,
                settings.StaleAfterHours,
                cancellationToken);

            _logger.LogInformation("Found {Count} shipments to update", shipmentsToUpdate.Count);

            var updatedCount = 0;
            var errorCount = 0;

            foreach (var shipment in shipmentsToUpdate)
            {
                try
                {
                    var updated = await UpdateShipmentTrackingAsync(
                        dbContext,
                        webhookDispatcher,
                        shipment,
                        cancellationToken);

                    if (updated)
                        updatedCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogWarning(ex,
                        "Failed to update tracking for shipment {ShipmentId} ({AwbNumber})",
                        shipment.Id, shipment.AwbNumber);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Shipment tracking update job completed. Updated: {Updated}, Errors: {Errors}",
                updatedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shipment tracking update job failed");
            throw;
        }
    }

    private async Task<List<ShipmentTrackingItem>> GetShipmentsForTrackingAsync(
        ITenantDbContext dbContext,
        int staleAfterHours,
        CancellationToken cancellationToken)
    {
        var staleTime = DateTime.UtcNow.AddHours(-staleAfterHours);
        var activeStatuses = new[]
        {
            ShipmentStatus.PickedUp,
            ShipmentStatus.InTransit,
            ShipmentStatus.OutForDelivery
        };

        return await dbContext.Shipments
            .AsNoTracking()
            .Where(s => activeStatuses.Contains(s.Status) &&
                       (s.UpdatedAt == null || s.UpdatedAt < staleTime))
            .OrderBy(s => s.UpdatedAt)
            .Select(s => new ShipmentTrackingItem
            {
                Id = s.Id,
                OrderId = s.OrderId,
                AwbNumber = s.AwbNumber,
                CourierType = s.CourierType,
                Status = s.Status,
                LastUpdated = s.UpdatedAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> UpdateShipmentTrackingAsync(
        ITenantDbContext dbContext,
        IWebhookDispatcher webhookDispatcher,
        ShipmentTrackingItem item,
        CancellationToken cancellationToken)
    {
        // In production, this would call the courier API to get tracking updates
        // For now, we log the intent and dispatch tracking webhook

        _logger.LogDebug(
            "Would fetch tracking from {CourierType} for AWB {AwbNumber}",
            item.CourierType, item.AwbNumber);

        // Simulate checking - in production, get actual tracking from courier adapter
        // The courier adapter would be resolved based on item.CourierType

        // Example: If status changed, we would update and dispatch webhook
        // var shipment = await dbContext.Shipments.FindAsync(item.Id);
        // shipment.UpdateStatus(newStatus, currentLocation, remarks);
        // await webhookDispatcher.DispatchAsync(WebhookEvent.ShipmentInTransit, ...);

        return false;
    }
}

/// <summary>
/// Internal DTO for shipment tracking processing.
/// </summary>
internal class ShipmentTrackingItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string AwbNumber { get; set; } = string.Empty;
    public CourierType CourierType { get; set; }
    public ShipmentStatus Status { get; set; }
    public DateTime? LastUpdated { get; set; }
}

/// <summary>
/// Arguments for shipment tracking update job.
/// </summary>
public class ShipmentTrackingJobArgs
{
    public int StaleAfterHours { get; set; } = 2;
}

/// <summary>
/// Configuration for shipment tracking update service.
/// </summary>
public class ShipmentTrackingSettings
{
    public const string SectionName = "ShipmentTracking";

    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 30;
    public int StaleAfterHours { get; set; } = 2;
}

/// <summary>
/// Background service that periodically updates shipment tracking status.
/// </summary>
public class ShipmentTrackingHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ShipmentTrackingHostedService> _logger;
    private readonly ShipmentTrackingSettings _settings;

    public ShipmentTrackingHostedService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<ShipmentTrackingSettings> settings,
        ILogger<ShipmentTrackingHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Shipment tracking service is disabled");
            return;
        }

        _logger.LogInformation(
            "Shipment tracking service started. Interval: {Interval} minutes",
            _settings.IntervalMinutes);

        // Initial delay
        await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<ShipmentTrackingUpdateJob>();

                await job.ExecuteAsync(new ShipmentTrackingJobArgs
                {
                    StaleAfterHours = _settings.StaleAfterHours
                }, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in shipment tracking cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Shipment tracking service stopped");
    }
}
