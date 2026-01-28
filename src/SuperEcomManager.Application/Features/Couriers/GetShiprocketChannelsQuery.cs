using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Query to get Shiprocket channels for a courier account.
/// </summary>
public record GetShiprocketChannelsQuery : IRequest<Result<List<ShiprocketChannelDto>>>, ITenantRequest
{
    public Guid CourierAccountId { get; init; }
}

public class GetShiprocketChannelsQueryHandler : IRequestHandler<GetShiprocketChannelsQuery, Result<List<ShiprocketChannelDto>>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IShiprocketChannelService _channelService;
    private readonly ILogger<GetShiprocketChannelsQueryHandler> _logger;

    public GetShiprocketChannelsQueryHandler(
        ITenantDbContext dbContext,
        IShiprocketChannelService channelService,
        ILogger<GetShiprocketChannelsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _channelService = channelService;
        _logger = logger;
    }

    public async Task<Result<List<ShiprocketChannelDto>>> Handle(
        GetShiprocketChannelsQuery request,
        CancellationToken cancellationToken)
    {
        var courierAccount = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.CourierAccountId && c.DeletedAt == null, cancellationToken);

        if (courierAccount == null)
        {
            return Result<List<ShiprocketChannelDto>>.Failure("Courier account not found");
        }

        if (courierAccount.CourierType != CourierType.Shiprocket)
        {
            return Result<List<ShiprocketChannelDto>>.Failure("Channel listing is only supported for Shiprocket accounts");
        }

        if (!courierAccount.IsConnected)
        {
            return Result<List<ShiprocketChannelDto>>.Failure("Courier account is not connected. Please test the connection first.");
        }

        if (string.IsNullOrEmpty(courierAccount.ApiKey) || string.IsNullOrEmpty(courierAccount.ApiSecret))
        {
            return Result<List<ShiprocketChannelDto>>.Failure("API credentials are not configured");
        }

        try
        {
            _logger.LogInformation("Fetching Shiprocket channels for courier account {CourierAccountId}",
                request.CourierAccountId);

            var channels = await _channelService.GetChannelsAsync(
                courierAccount.ApiKey,
                courierAccount.ApiSecret,
                cancellationToken);

            _logger.LogInformation("Retrieved {ChannelCount} channels from Shiprocket for account {CourierAccountId}",
                channels.Count, request.CourierAccountId);

            return Result<List<ShiprocketChannelDto>>.Success(channels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Shiprocket channels for courier account {CourierAccountId}",
                request.CourierAccountId);
            return Result<List<ShiprocketChannelDto>>.Failure($"Error fetching channels: {ex.Message}");
        }
    }
}
