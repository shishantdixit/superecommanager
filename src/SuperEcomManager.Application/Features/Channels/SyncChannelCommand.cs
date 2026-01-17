using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to trigger manual sync for a channel.
/// </summary>
[RequirePermission("channels.sync")]
[RequireFeature("channels_management")]
public record SyncChannelCommand : IRequest<Result<ChannelSyncResult>>, ITenantRequest
{
    public Guid ChannelId { get; init; }
}

/// <summary>
/// Result of a channel sync operation.
/// </summary>
public class ChannelSyncResult
{
    public Guid ChannelId { get; set; }
    public int OrdersImported { get; set; }
    public int OrdersUpdated { get; set; }
    public int OrdersFailed { get; set; }
    public DateTime SyncedAt { get; set; }
    public string Status { get; set; } = "Completed";
    public List<string> Errors { get; set; } = new();
}

public class SyncChannelCommandHandler : IRequestHandler<SyncChannelCommand, Result<ChannelSyncResult>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IChannelSyncServiceFactory _syncServiceFactory;
    private readonly ILogger<SyncChannelCommandHandler> _logger;

    public SyncChannelCommandHandler(
        ITenantDbContext dbContext,
        IChannelSyncServiceFactory syncServiceFactory,
        ILogger<SyncChannelCommandHandler> logger)
    {
        _dbContext = dbContext;
        _syncServiceFactory = syncServiceFactory;
        _logger = logger;
    }

    public async Task<Result<ChannelSyncResult>> Handle(SyncChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

        if (channel == null)
        {
            return Result<ChannelSyncResult>.Failure("Channel not found");
        }

        if (!channel.IsConnected)
        {
            return Result<ChannelSyncResult>.Failure("Channel is not connected. Please complete the connection setup first.");
        }

        // Check for credentials - AccessToken for OAuth-based channels, CredentialsEncrypted for others
        var hasCredentials = !string.IsNullOrEmpty(channel.AccessToken) || !string.IsNullOrEmpty(channel.CredentialsEncrypted);
        if (!hasCredentials)
        {
            return Result<ChannelSyncResult>.Failure("Channel credentials are missing. Please reconnect.");
        }

        // Get the appropriate sync service for this channel type
        var syncService = _syncServiceFactory.GetService(channel.Type);
        if (syncService == null)
        {
            return Result<ChannelSyncResult>.Failure($"Sync not supported for channel type: {channel.Type}");
        }

        _logger.LogInformation("Starting manual sync for channel {ChannelId} (type: {ChannelType})",
            request.ChannelId, channel.Type);

        try
        {
            var result = await syncService.SyncOrdersAsync(request.ChannelId, cancellationToken: cancellationToken);

            channel.RecordSync(result.Status != "Failed", result.Status);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sync completed for channel {ChannelId}: {OrdersImported} imported, {OrdersUpdated} updated, {OrdersFailed} failed",
                request.ChannelId, result.OrdersImported, result.OrdersUpdated, result.OrdersFailed);

            return Result<ChannelSyncResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for channel {ChannelId}", request.ChannelId);
            channel.RecordSync(false, ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result<ChannelSyncResult>.Failure($"Sync failed: {ex.Message}");
        }
    }
}
