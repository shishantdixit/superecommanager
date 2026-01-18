using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Query to get all sales channels for the current tenant.
/// </summary>
[RequirePermission("channels.view")]
[RequireFeature("channels_management")]
public record GetChannelsQuery : IRequest<IReadOnlyList<ChannelDto>>, ITenantRequest;

public class GetChannelsQueryHandler : IRequestHandler<GetChannelsQuery, IReadOnlyList<ChannelDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetChannelsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ChannelDto>> Handle(GetChannelsQuery request, CancellationToken cancellationToken)
    {
        var channels = await _dbContext.SalesChannels
            .AsNoTracking()
            .Where(c => c.IsActive) // Only show active channels
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
                LastError = c.LastError,
                InitialSyncDays = c.InitialSyncDays,
                InventorySyncDays = c.InventorySyncDays,
                ProductSyncDays = c.ProductSyncDays,
                OrderSyncLimit = c.OrderSyncLimit,
                InventorySyncLimit = c.InventorySyncLimit,
                ProductSyncLimit = c.ProductSyncLimit,
                SyncProductsEnabled = c.SyncProductsEnabled,
                AutoSyncProducts = c.AutoSyncProducts,
                LastProductSyncAt = c.LastProductSyncAt,
                LastInventorySyncAt = c.LastInventorySyncAt
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return channels;
    }
}
