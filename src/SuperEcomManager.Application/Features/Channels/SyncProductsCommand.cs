using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to trigger manual product sync for a channel.
/// </summary>
[RequirePermission("channels.sync")]
[RequireFeature("channels_management")]
public record SyncProductsCommand : IRequest<Result<ChannelSyncResult>>, ITenantRequest
{
    public Guid ChannelId { get; init; }
}

public class SyncProductsCommandHandler : IRequestHandler<SyncProductsCommand, Result<ChannelSyncResult>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IChannelSyncServiceFactory _syncServiceFactory;
    private readonly ILogger<SyncProductsCommandHandler> _logger;

    public SyncProductsCommandHandler(
        ITenantDbContext dbContext,
        IChannelSyncServiceFactory syncServiceFactory,
        ILogger<SyncProductsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _syncServiceFactory = syncServiceFactory;
        _logger = logger;
    }

    public async Task<Result<ChannelSyncResult>> Handle(SyncProductsCommand request, CancellationToken cancellationToken)
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

        // Check for credentials
        var hasCredentials = !string.IsNullOrEmpty(channel.AccessToken) || !string.IsNullOrEmpty(channel.CredentialsEncrypted);
        if (!hasCredentials)
        {
            return Result<ChannelSyncResult>.Failure("Channel credentials are missing. Please reconnect.");
        }

        // Check if product sync is enabled for this channel
        if (!channel.SyncProductsEnabled)
        {
            return Result<ChannelSyncResult>.Failure("Product sync is not enabled for this channel. Please enable it in channel settings.");
        }

        // Get the appropriate sync service for this channel type
        var syncService = _syncServiceFactory.GetService(channel.Type);
        if (syncService == null)
        {
            return Result<ChannelSyncResult>.Failure($"Product sync not supported for channel type: {channel.Type}");
        }

        _logger.LogInformation("Starting manual product sync for channel {ChannelId} (type: {ChannelType})",
            request.ChannelId, channel.Type);

        try
        {
            var result = await syncService.SyncProductsAsync(request.ChannelId, cancellationToken);

            // Record the sync
            channel.RecordProductSync();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product sync completed for channel {ChannelId}: {Imported} imported, {Updated} updated, {Failed} failed",
                request.ChannelId, result.ProductsImported, result.ProductsUpdated, result.ProductsFailed);

            return Result<ChannelSyncResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product sync failed for channel {ChannelId}", request.ChannelId);
            return Result<ChannelSyncResult>.Failure($"Product sync failed: {ex.Message}");
        }
    }
}
