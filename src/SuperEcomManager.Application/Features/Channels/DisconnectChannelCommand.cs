using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Command to disconnect a sales channel.
/// </summary>
[RequirePermission("channels.manage")]
[RequireFeature("channels_management")]
public record DisconnectChannelCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid ChannelId { get; init; }
}

public class DisconnectChannelCommandHandler : IRequestHandler<DisconnectChannelCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<DisconnectChannelCommandHandler> _logger;

    public DisconnectChannelCommandHandler(
        ITenantDbContext dbContext,
        ILogger<DisconnectChannelCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DisconnectChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

        if (channel == null)
        {
            return Result<bool>.Failure("Channel not found");
        }

        // Deactivate the channel (soft delete approach)
        channel.Deactivate();

        // Clear credentials for security
        channel.UpdateCredentials(string.Empty);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Disconnected channel {ChannelId} ({ChannelName})", channel.Id, channel.Name);

        return Result<bool>.Success(true);
    }
}
