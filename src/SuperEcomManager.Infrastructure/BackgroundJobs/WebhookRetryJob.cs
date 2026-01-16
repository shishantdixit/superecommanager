using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Infrastructure.Persistence;

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
        _logger.LogDebug("Starting webhook retry job");

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

            // Process webhook retries for each tenant
            foreach (var tenant in tenants)
            {
                try
                {
                    await ProcessTenantWebhookRetriesAsync(
                        tenant.Id, tenant.SchemaName, tenant.Slug, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to process webhook retries for tenant {TenantId}", tenant.Id);
                }
            }

            _logger.LogDebug("Webhook retry job completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook retry job failed");
            throw;
        }
    }

    private async Task ProcessTenantWebhookRetriesAsync(
        Guid tenantId, string schemaName, string slug, CancellationToken cancellationToken)
    {
        // Create a new scope for this tenant
        using var tenantScope = _serviceProvider.CreateScope();

        // Set the tenant context
        var currentTenantService = tenantScope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        currentTenantService.SetTenant(tenantId, schemaName, slug);

        var webhookDispatcher = tenantScope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

        await webhookDispatcher.RetryFailedDeliveriesAsync(cancellationToken);
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
