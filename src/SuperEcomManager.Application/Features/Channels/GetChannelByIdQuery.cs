using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Query to get a sales channel by ID.
/// </summary>
[RequirePermission("channels.view")]
[RequireFeature("channels_management")]
public record GetChannelByIdQuery : IRequest<ChannelDto?>, ITenantRequest
{
    public Guid Id { get; init; }
}

public class GetChannelByIdQueryHandler : IRequestHandler<GetChannelByIdQuery, ChannelDto?>
{
    private readonly ITenantDbContext _dbContext;

    public GetChannelByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ChannelDto?> Handle(GetChannelByIdQuery request, CancellationToken cancellationToken)
    {
        var channel = await _dbContext.SalesChannels
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new ChannelDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                IsActive = c.IsActive,
                StoreUrl = c.StoreUrl,
                StoreName = c.StoreName,
                LastSyncAt = c.LastSyncAt,
                TotalOrders = _dbContext.Orders.Count(o => o.ChannelId == c.Id),
                SyncStatus = c.LastSyncAt.HasValue ? ChannelSyncStatus.Completed : ChannelSyncStatus.NotStarted,
                CreatedAt = c.CreatedAt,
                AutoSyncOrders = c.AutoSyncOrders,
                AutoSyncInventory = c.AutoSyncInventory,
                IsConnected = c.IsConnected,
                HasCredentials = c.ApiKey != null,
                LastError = c.LastError
            })
            .FirstOrDefaultAsync(cancellationToken);

        return channel;
    }
}
