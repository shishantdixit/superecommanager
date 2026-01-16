using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that sends pending notifications.
/// </summary>
public class NotificationSenderJob : IBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationSenderJob> _logger;

    public NotificationSenderJob(
        IServiceProvider serviceProvider,
        ILogger<NotificationSenderJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(object? args, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting notification sender job");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITenantDbContext>();

            // Get pending notifications
            var pendingNotifications = await dbContext.NotificationLogs
                .Where(n => n.Status == "Pending")
                .OrderBy(n => n.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            if (pendingNotifications.Count == 0)
            {
                _logger.LogDebug("No pending notifications to send");
                return;
            }

            _logger.LogInformation("Processing {Count} pending notifications", pendingNotifications.Count);

            var successCount = 0;
            var failedCount = 0;

            foreach (var notification in pendingNotifications)
            {
                try
                {
                    // In production, this would call actual notification services
                    // For now, we just mark as sent
                    var success = await SendNotificationAsync(notification.Type, notification.Recipient, notification.Content);

                    if (success)
                    {
                        notification.MarkSent(null);
                        successCount++;
                    }
                    else
                    {
                        notification.MarkFailed("Delivery failed");
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    notification.MarkFailed(ex.Message);
                    failedCount++;
                    _logger.LogWarning(ex, "Failed to send notification {NotificationId}", notification.Id);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Notification sender job completed. Sent: {SuccessCount}, Failed: {FailedCount}",
                successCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification sender job failed");
            throw;
        }
    }

    private Task<bool> SendNotificationAsync(Domain.Enums.NotificationType type, string recipient, string content)
    {
        // In production, this would integrate with:
        // - SMS providers (Twilio, etc.)
        // - Email providers (SendGrid, SES, etc.)
        // - WhatsApp Business API
        // - Push notification services

        _logger.LogDebug(
            "Would send {Type} notification to {Recipient}: {ContentPreview}",
            type, recipient, content.Length > 50 ? content[..50] + "..." : content);

        return Task.FromResult(true);
    }
}

/// <summary>
/// Configuration for notification sender service.
/// </summary>
public class NotificationSenderSettings
{
    public const string SectionName = "NotificationSender";

    /// <summary>
    /// Whether automatic sending is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between send runs in seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum notifications to process per batch.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

/// <summary>
/// Background service that periodically sends pending notifications.
/// </summary>
public class NotificationSenderHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationSenderHostedService> _logger;
    private readonly NotificationSenderSettings _settings;

    public NotificationSenderHostedService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<NotificationSenderSettings> settings,
        ILogger<NotificationSenderHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Notification sender service is disabled");
            return;
        }

        _logger.LogInformation(
            "Notification sender service started. Interval: {Interval} seconds",
            _settings.IntervalSeconds);

        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<NotificationSenderJob>();
                await job.ExecuteAsync(null, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in notification sender cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Notification sender service stopped");
    }
}
