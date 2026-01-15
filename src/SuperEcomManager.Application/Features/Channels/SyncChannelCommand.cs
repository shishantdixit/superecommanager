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
    private readonly ILogger<SyncChannelCommandHandler> _logger;

    // Will be injected from infrastructure to perform actual sync
    public Func<Guid, CancellationToken, Task<ChannelSyncResult>>? PerformSync { get; set; }

    public SyncChannelCommandHandler(
        ITenantDbContext dbContext,
        ILogger<SyncChannelCommandHandler> logger)
    {
        _dbContext = dbContext;
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

        if (!channel.IsActive)
        {
            return Result<ChannelSyncResult>.Failure("Channel is not active. Please reconnect first.");
        }

        if (string.IsNullOrEmpty(channel.CredentialsEncrypted))
        {
            return Result<ChannelSyncResult>.Failure("Channel credentials are missing. Please reconnect.");
        }

        _logger.LogInformation("Starting manual sync for channel {ChannelId}", request.ChannelId);

        try
        {
            ChannelSyncResult result;

            if (PerformSync != null)
            {
                result = await PerformSync(request.ChannelId, cancellationToken);
            }
            else
            {
                // Fallback: mark sync as triggered (actual sync will be handled by background job)
                result = new ChannelSyncResult
                {
                    ChannelId = request.ChannelId,
                    OrdersImported = 0,
                    OrdersUpdated = 0,
                    OrdersFailed = 0,
                    SyncedAt = DateTime.UtcNow,
                    Status = "Queued"
                };
            }

            channel.RecordSync(result.Status != "Failed", result.Status);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sync completed for channel {ChannelId}: {OrdersImported} imported, {OrdersUpdated} updated",
                request.ChannelId, result.OrdersImported, result.OrdersUpdated);

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
