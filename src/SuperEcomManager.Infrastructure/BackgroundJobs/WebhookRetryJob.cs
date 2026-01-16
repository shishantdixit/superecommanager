using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that retries failed webhook deliveries.
/// </summary>
public class WebhookRetryJob : IBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookRetryJob> _logger;

    public WebhookRetryJob(
        IServiceProvider serviceProvider,
        ILogger<WebhookRetryJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting webhook retry job");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var webhookDispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

            await webhookDispatcher.RetryFailedDeliveriesAsync(cancellationToken);

            _logger.LogInformation("Webhook retry job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook retry job failed");
            throw;
        }
    }
}

/// <summary>
/// Configuration for webhook retry service.
/// </summary>
public class WebhookRetrySettings
{
    public const string SectionName = "WebhookRetry";

    /// <summary>
    /// Whether automatic retry is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between retry runs in minutes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 5;
}

/// <summary>
/// Background service that periodically retries failed webhook deliveries.
/// </summary>
public class WebhookRetryHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookRetryHostedService> _logger;
    private readonly WebhookRetrySettings _settings;

    public WebhookRetryHostedService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<WebhookRetrySettings> settings,
        ILogger<WebhookRetryHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Webhook retry service is disabled");
            return;
        }

        _logger.LogInformation(
            "Webhook retry service started. Interval: {Interval} minutes",
            _settings.IntervalMinutes);

        // Wait before first run
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<WebhookRetryJob>();
                await job.ExecuteAsync(null, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in webhook retry cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Webhook retry service stopped");
    }
}
