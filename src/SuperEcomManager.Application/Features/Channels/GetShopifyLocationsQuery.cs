using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Channels;

/// <summary>
/// Query to get Shopify locations for a channel.
/// Used to verify read_locations permission is working.
/// </summary>
[RequirePermission("channels.view")]
[RequireFeature("channels_management")]
public record GetShopifyLocationsQuery : IRequest<Result<List<ShopifyLocationDto>>>, ITenantRequest
{
    public Guid ChannelId { get; init; }
}

public class ShopifyLocationDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Country { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }
    public bool Active { get; set; }
    public bool Legacy { get; set; }
}

public class GetShopifyLocationsQueryHandler : IRequestHandler<GetShopifyLocationsQuery, Result<List<ShopifyLocationDto>>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IChannelSyncServiceFactory _syncServiceFactory;
    private readonly ILogger<GetShopifyLocationsQueryHandler> _logger;

    public GetShopifyLocationsQueryHandler(
        ITenantDbContext dbContext,
        IChannelSyncServiceFactory syncServiceFactory,
        ILogger<GetShopifyLocationsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _syncServiceFactory = syncServiceFactory;
        _logger = logger;
    }

    public async Task<Result<List<ShopifyLocationDto>>> Handle(
        GetShopifyLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var channel = await _dbContext.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

        if (channel == null)
        {
            return Result<List<ShopifyLocationDto>>.Failure("Channel not found");
        }

        if (!channel.IsConnected)
        {
            return Result<List<ShopifyLocationDto>>.Failure("Channel is not connected. Please connect first.");
        }

        var syncService = _syncServiceFactory.GetService(channel.Type);
        if (syncService == null)
        {
            return Result<List<ShopifyLocationDto>>.Failure($"Sync service not found for channel type {channel.Type}");
        }

        try
        {
            var locations = await syncService.GetLocationsAsync(request.ChannelId, cancellationToken);

            _logger.LogInformation("Retrieved {Count} locations for channel {ChannelId}",
                locations.Count, request.ChannelId);

            var locationDtos = locations.Select(l => new ShopifyLocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Address1 = l.Address1,
                Address2 = l.Address2,
                City = l.City,
                Province = l.Province,
                Country = l.Country,
                Zip = l.Zip,
                Phone = l.Phone,
                Active = l.Active,
                Legacy = l.Legacy
            }).ToList();

            return Result<List<ShopifyLocationDto>>.Success(locationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get locations for channel {ChannelId}", request.ChannelId);
            return Result<List<ShopifyLocationDto>>.Failure($"Failed to get locations: {ex.Message}");
        }
    }
}
