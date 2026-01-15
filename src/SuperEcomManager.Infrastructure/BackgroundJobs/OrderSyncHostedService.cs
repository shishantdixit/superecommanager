using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Configuration for the order sync background service.
/// </summary>
public class OrderSyncSettings
{
    public const string SectionName = "OrderSync";

    /// <summary>
    /// Whether automatic sync is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between sync runs in minutes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// How far back to look for orders (in hours).
    /// </summary>
    public int LookbackHours { get; set; } = 24;
}

/// <summary>
/// Background service that periodically syncs orders from all active sales channels.
/// </summary>
public class OrderSyncHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderSyncHostedService> _logger;
    private readonly OrderSyncSettings _settings;

    public OrderSyncHostedService(
        IServiceProvider serviceProvider,
        IOptions<OrderSyncSettings> settings,
        ILogger<OrderSyncHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Order sync service is disabled");
            return;
        }

        _logger.LogInformation(
            "Order sync service started. Interval: {Interval} minutes",
            _settings.IntervalMinutes);

        // Wait a bit before first run to let the app fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSyncCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in order sync cycle");
            }

            // Wait for next interval
            await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Order sync service stopped");
    }

    private async Task RunSyncCycleAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting order sync cycle");

        using var scope = _serviceProvider.CreateScope();

        try
        {
            var job = scope.ServiceProvider.GetRequiredService<OrderSyncJob>();
            var fromDate = DateTime.UtcNow.AddHours(-_settings.LookbackHours);

            await job.ExecuteAsync(new ChannelSyncJobArgs
            {
                FromDate = fromDate,
                ToDate = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogDebug("Order sync cycle completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order sync cycle failed");
            throw;
        }
    }
}
