using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that checks inventory levels and sends alerts for low stock items.
/// </summary>
public class StockAlertJob : IBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockAlertJob> _logger;

    public StockAlertJob(
        IServiceProvider serviceProvider,
        ILogger<StockAlertJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        var settings = args as StockAlertJobArgs ?? new StockAlertJobArgs();

        _logger.LogInformation("Starting stock alert job");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITenantDbContext>();
            var webhookDispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

            // Check for low stock items using inventory locations
            var lowStockItems = await GetLowStockItemsAsync(dbContext, settings.DefaultLowStockThreshold, cancellationToken);

            if (lowStockItems.Count > 0)
            {
                _logger.LogInformation("Found {Count} low stock items", lowStockItems.Count);

                // Dispatch individual alerts for critical items
                foreach (var item in lowStockItems.Where(i => i.Quantity <= 0))
                {
                    await webhookDispatcher.DispatchAsync(
                        WebhookEvent.InventoryOutOfStock,
                        new
                        {
                            ProductId = item.ProductId,
                            Sku = item.Sku,
                            ProductName = item.ProductName,
                            Location = item.Location,
                            CurrentStock = item.Quantity,
                            ReorderLevel = item.ReorderLevel
                        },
                        cancellationToken);
                }

                // Dispatch summary alert for low stock
                var lowStockSummary = lowStockItems
                    .Where(i => i.Quantity > 0)
                    .Take(20)
                    .ToList();

                if (lowStockSummary.Count > 0)
                {
                    await webhookDispatcher.DispatchAsync(
                        WebhookEvent.InventoryLow,
                        new
                        {
                            TotalLowStockItems = lowStockItems.Count(i => i.Quantity > 0),
                            Items = lowStockSummary.Select(i => new
                            {
                                ProductId = i.ProductId,
                                Sku = i.Sku,
                                ProductName = i.ProductName,
                                CurrentStock = i.Quantity,
                                ReorderLevel = i.ReorderLevel
                            })
                        },
                        cancellationToken);
                }
            }
            else
            {
                _logger.LogDebug("No low stock items found");
            }

            _logger.LogInformation("Stock alert job completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock alert job failed");
            throw;
        }
    }

    private async Task<List<LowStockItem>> GetLowStockItemsAsync(
        ITenantDbContext dbContext,
        int defaultThreshold,
        CancellationToken cancellationToken)
    {
        // Query inventory items and join with products
        var lowStockItems = await dbContext.Inventory
            .AsNoTracking()
            .Include(i => i.Product)
            .Where(i => i.Product != null &&
                        i.Product.IsActive &&
                        i.QuantityOnHand <= (i.ReorderPoint > 0 ? i.ReorderPoint : defaultThreshold))
            .Select(i => new LowStockItem
            {
                ProductId = i.ProductId,
                Sku = i.Sku,
                ProductName = i.Product!.Name,
                Location = i.Location ?? "Default",
                Quantity = i.QuantityOnHand,
                ReorderLevel = i.ReorderPoint > 0 ? i.ReorderPoint : defaultThreshold
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return lowStockItems;
    }
}

/// <summary>
/// Internal DTO for low stock item processing.
/// </summary>
internal class LowStockItem
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
}

/// <summary>
/// Arguments for stock alert job.
/// </summary>
public class StockAlertJobArgs
{
    public int DefaultLowStockThreshold { get; set; } = 10;
}

/// <summary>
/// Configuration for stock alert service.
/// </summary>
public class StockAlertSettings
{
    public const string SectionName = "StockAlert";

    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 6;
    public int DefaultLowStockThreshold { get; set; } = 10;
}

/// <summary>
/// Background service that periodically checks stock levels and sends alerts.
/// </summary>
public class StockAlertHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockAlertHostedService> _logger;
    private readonly StockAlertSettings _settings;

    public StockAlertHostedService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<StockAlertSettings> settings,
        ILogger<StockAlertHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Stock alert service is disabled");
            return;
        }

        _logger.LogInformation(
            "Stock alert service started. Interval: {Interval} hours",
            _settings.IntervalHours);

        // Initial delay before first check
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<StockAlertJob>();

                await job.ExecuteAsync(new StockAlertJobArgs
                {
                    DefaultLowStockThreshold = _settings.DefaultLowStockThreshold
                }, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in stock alert cycle");
            }

            await Task.Delay(TimeSpan.FromHours(_settings.IntervalHours), stoppingToken);
        }

        _logger.LogInformation("Stock alert service stopped");
    }
}
