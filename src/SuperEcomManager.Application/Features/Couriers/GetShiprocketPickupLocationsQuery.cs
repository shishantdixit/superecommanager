using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Query to get Shiprocket pickup locations for a courier account.
/// </summary>
public record GetShiprocketPickupLocationsQuery : IRequest<Result<List<ShiprocketPickupLocationDto>>>, ITenantRequest
{
    public Guid CourierAccountId { get; init; }
}

public class GetShiprocketPickupLocationsQueryHandler
    : IRequestHandler<GetShiprocketPickupLocationsQuery, Result<List<ShiprocketPickupLocationDto>>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IShiprocketChannelService _channelService;
    private readonly ILogger<GetShiprocketPickupLocationsQueryHandler> _logger;

    public GetShiprocketPickupLocationsQueryHandler(
        ITenantDbContext dbContext,
        IShiprocketChannelService channelService,
        ILogger<GetShiprocketPickupLocationsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _channelService = channelService;
        _logger = logger;
    }

    public async Task<Result<List<ShiprocketPickupLocationDto>>> Handle(
        GetShiprocketPickupLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var courierAccount = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.CourierAccountId && c.DeletedAt == null, cancellationToken);

        if (courierAccount == null)
        {
            return Result<List<ShiprocketPickupLocationDto>>.Failure("Courier account not found");
        }

        if (courierAccount.CourierType != CourierType.Shiprocket)
        {
            return Result<List<ShiprocketPickupLocationDto>>.Failure("Pickup location listing is only supported for Shiprocket accounts");
        }

        if (!courierAccount.IsConnected)
        {
            return Result<List<ShiprocketPickupLocationDto>>.Failure("Courier account is not connected. Please test the connection first.");
        }

        if (string.IsNullOrEmpty(courierAccount.ApiKey) || string.IsNullOrEmpty(courierAccount.ApiSecret))
        {
            return Result<List<ShiprocketPickupLocationDto>>.Failure("API credentials are not configured");
        }

        try
        {
            _logger.LogInformation("Fetching Shiprocket pickup locations for courier account {CourierAccountId}",
                request.CourierAccountId);

            var locations = await _channelService.GetPickupLocationsAsync(
                courierAccount.ApiKey,
                courierAccount.ApiSecret,
                cancellationToken);

            _logger.LogInformation("Retrieved {LocationCount} pickup locations from Shiprocket for account {CourierAccountId}",
                locations.Count, request.CourierAccountId);

            return Result<List<ShiprocketPickupLocationDto>>.Success(locations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Shiprocket pickup locations for courier account {CourierAccountId}",
                request.CourierAccountId);
            return Result<List<ShiprocketPickupLocationDto>>.Failure($"Error fetching pickup locations: {ex.Message}");
        }
    }
}
