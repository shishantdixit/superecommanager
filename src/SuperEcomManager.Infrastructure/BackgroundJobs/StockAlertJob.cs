using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Infrastructure.Persistence;

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

        _logger.LogDebug("Starting stock alert job");

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

            var totalLowStockItems = 0;

            // Process stock alerts for each tenant
            foreach (var tenant in tenants)
            {
                try
                {
                    var lowStockCount = await ProcessTenantStockAlertsAsync(
                        tenant.Id, tenant.SchemaName, tenant.Slug, settings, cancellationToken);
                    totalLowStockItems += lowStockCount;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to process stock alerts for tenant {TenantId}", tenant.Id);
                }
            }

            if (totalLowStockItems > 0)
            {
                _logger.LogInformation("Stock alert job completed. Total low stock items: {Count}", totalLowStockItems);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock alert job failed");
            throw;
        }
    }

    private async Task<int> ProcessTenantStockAlertsAsync(
        Guid tenantId, string schemaName, string slug, StockAlertJobArgs settings, CancellationToken cancellationToken)
    {
        // Create a new scope for this tenant
        using var tenantScope = _serviceProvider.CreateScope();

        // Set the tenant context
        var currentTenantService = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        currentTenantService.SetTenant(tenantId, schemaName, slug);

        var dbContext = tenantScope.ServiceProvider.GetRequiredService<ITenantDbContext>();
        var webhookDispatcher = tenantScope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

        // Check for low stock items using inventory locations
        var lowStockItems = await GetLowStockItemsAsync(dbContext, settings.DefaultLowStockThreshold, cancellationToken);

        if (lowStockItems.Count > 0)
        {
            _logger.LogDebug("Found {Count} low stock items for tenant {TenantId}", lowStockItems.Count, tenantId);

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

        return lowStockItems.Count;
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
