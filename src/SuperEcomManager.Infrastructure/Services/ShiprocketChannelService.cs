using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Integrations.Couriers.Shiprocket;
using SuperEcomManager.Integrations.Couriers.Shiprocket.Models;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Service for fetching Shiprocket channel information.
/// </summary>
public class ShiprocketChannelService : IShiprocketChannelService
{
    private readonly IShiprocketClient _shiprocketClient;
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<ShiprocketChannelService> _logger;

    public ShiprocketChannelService(
        IShiprocketClient shiprocketClient,
        ITenantDbContext dbContext,
        ILogger<ShiprocketChannelService> logger)
    {
        _shiprocketClient = shiprocketClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<ShiprocketChannelDto>> GetChannelsAsync(
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Shiprocket channels for {Email}", apiKey);

        // Authenticate (requires API user credentials)
        var authResponse = await _shiprocketClient.AuthenticateAsync(apiKey, apiSecret, cancellationToken);

        if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
        {
            _logger.LogError("Failed to authenticate with Shiprocket for {Email}", apiKey);
            throw new InvalidOperationException("Failed to authenticate with Shiprocket");
        }

        // Get channels
        var channelsResponse = await _shiprocketClient.GetChannelsAsync(authResponse.Token, cancellationToken);

        if (channelsResponse?.Data == null)
        {
            _logger.LogWarning("No channels returned from Shiprocket for {Email}", apiKey);
            return new List<ShiprocketChannelDto>();
        }

        var channels = channelsResponse.Data
            .Select(c => new ShiprocketChannelDto
            {
                Id = c.Id,
                Name = c.Name ?? $"Channel {c.Id}",
                Type = c.Type
            })
            .OrderBy(c => c.Name)
            .ToList();

        _logger.LogInformation("Retrieved {ChannelCount} channels from Shiprocket for {Email}",
            channels.Count, apiKey);

        return channels;
    }

    public async Task<List<ShiprocketPickupLocationDto>> GetPickupLocationsAsync(
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Shiprocket pickup locations for {Email}", apiKey);

        // Authenticate (requires API user credentials)
        var authResponse = await _shiprocketClient.AuthenticateAsync(apiKey, apiSecret, cancellationToken);

        if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
        {
            _logger.LogError("Failed to authenticate with Shiprocket for {Email}", apiKey);
            throw new InvalidOperationException("Failed to authenticate with Shiprocket");
        }

        // Get pickup locations
        var locationsResponse = await _shiprocketClient.GetPickupLocationsAsync(authResponse.Token, cancellationToken);

        if (locationsResponse?.Data?.ShippingAddress == null)
        {
            _logger.LogWarning("No pickup locations returned from Shiprocket for {Email}", apiKey);
            return new List<ShiprocketPickupLocationDto>();
        }

        var locations = locationsResponse.Data.ShippingAddress
            .Select(l => new ShiprocketPickupLocationDto
            {
                Id = l.Id,
                Name = l.PickupLocation ?? $"Location {l.Id}",
                Address = l.Address,
                City = l.City,
                State = l.State,
                PinCode = l.PinCode,
                Phone = l.Phone,
                IsActive = l.Status == 1
            })
            .OrderBy(l => l.Name)
            .ToList();

        _logger.LogInformation("Retrieved {LocationCount} pickup locations from Shiprocket for {Email}",
            locations.Count, apiKey);

        return locations;
    }

    public async Task<ServiceabilityResult> CheckServiceabilityAsync(
        Guid courierAccountId,
        string pickupPincode,
        string deliveryPincode,
        decimal weight,
        bool isCod,
        long? orderId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get courier account
            var courierAccount = await _dbContext.CourierAccounts
                .FirstOrDefaultAsync(ca => ca.Id == courierAccountId && ca.DeletedAt == null, cancellationToken);

            if (courierAccount == null)
            {
                return new ServiceabilityResult
                {
                    Success = false,
                    ErrorMessage = "Courier account not found"
                };
            }

            if (string.IsNullOrEmpty(courierAccount.ApiKey) || string.IsNullOrEmpty(courierAccount.ApiSecret))
            {
                return new ServiceabilityResult
                {
                    Success = false,
                    ErrorMessage = "Courier account credentials not configured"
                };
            }

            // Authenticate
            var authResponse = await _shiprocketClient.AuthenticateAsync(
                courierAccount.ApiKey,
                courierAccount.ApiSecret,
                cancellationToken);

            if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
            {
                return new ServiceabilityResult
                {
                    Success = false,
                    ErrorMessage = "Failed to authenticate with Shiprocket"
                };
            }

            // Check serviceability
            var serviceabilityResponse = await _shiprocketClient.CheckServiceabilityAsync(
                authResponse.Token,
                pickupPincode,
                deliveryPincode,
                weight,
                isCod,
                orderId,
                cancellationToken);

            if (serviceabilityResponse == null)
            {
                return new ServiceabilityResult
                {
                    Success = false,
                    ErrorMessage = "Failed to check serviceability - no response from Shiprocket"
                };
            }

            if (serviceabilityResponse.Data?.AvailableCourierCompanies == null ||
                !serviceabilityResponse.Data.AvailableCourierCompanies.Any())
            {
                return new ServiceabilityResult
                {
                    Success = true,
                    AvailableCouriers = new List<AvailableCourierInfo>(),
                    ErrorMessage = "No couriers available for this route"
                };
            }

            var couriers = serviceabilityResponse.Data.AvailableCourierCompanies
                .Where(c => c.Blocked == 0) // Only unblocked couriers
                .Select(c => new AvailableCourierInfo
                {
                    CourierId = c.Id,
                    CourierName = c.Name,
                    FreightCharge = c.FreightCharge,
                    CodCharges = c.CodCharges,
                    EstimatedDeliveryDays = c.EstimatedDeliveryDays,
                    Etd = c.Etd,
                    Rating = c.Rating,
                    IsSurface = c.IsSurface
                })
                .ToList();

            _logger.LogInformation(
                "Serviceability check successful: {Count} couriers available, recommended: {RecommendedId}",
                couriers.Count, serviceabilityResponse.Data.RecommendedCourierId);

            return new ServiceabilityResult
            {
                Success = true,
                RecommendedCourierId = serviceabilityResponse.Data.RecommendedCourierId,
                AvailableCouriers = couriers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking serviceability for courier account {AccountId}", courierAccountId);
            return new ServiceabilityResult
            {
                Success = false,
                ErrorMessage = $"Error checking serviceability: {ex.Message}"
            };
        }
    }

    public async Task<AwbGenerationResult> GenerateAwbAsync(
        Guid courierAccountId,
        long shipmentId,
        int? courierId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get courier account
            var courierAccount = await _dbContext.CourierAccounts
                .FirstOrDefaultAsync(ca => ca.Id == courierAccountId && ca.DeletedAt == null, cancellationToken);

            if (courierAccount == null)
            {
                return new AwbGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Courier account not found"
                };
            }

            if (string.IsNullOrEmpty(courierAccount.ApiKey) || string.IsNullOrEmpty(courierAccount.ApiSecret))
            {
                return new AwbGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Courier account credentials not configured"
                };
            }

            // Authenticate
            var authResponse = await _shiprocketClient.AuthenticateAsync(
                courierAccount.ApiKey,
                courierAccount.ApiSecret,
                cancellationToken);

            if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
            {
                return new AwbGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to authenticate with Shiprocket"
                };
            }

            // Generate AWB
            var awbRequest = new ShiprocketGenerateAwbRequest
            {
                ShipmentId = shipmentId,
                CourierId = courierId
            };

            _logger.LogInformation(
                "Generating AWB for shipment {ShipmentId} with courier {CourierId}",
                shipmentId, courierId?.ToString() ?? "auto");

            var awbResponse = await _shiprocketClient.GenerateAwbAsync(
                authResponse.Token,
                awbRequest,
                cancellationToken);

            if (awbResponse == null)
            {
                return new AwbGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to generate AWB - no response from Shiprocket"
                };
            }

            if (awbResponse.AwbAssignStatus != 1)
            {
                return new AwbGenerationResult
                {
                    Success = false,
                    ErrorMessage = "AWB assignment failed. The courier may not be serviceable or wallet balance may be insufficient."
                };
            }

            var awbData = awbResponse.Response?.Data;
            if (awbData == null || string.IsNullOrEmpty(awbData.AwbCode))
            {
                return new AwbGenerationResult
                {
                    Success = false,
                    ErrorMessage = "AWB generated but no AWB code returned"
                };
            }

            // Generate label URL
            var labelResponse = await _shiprocketClient.GetLabelAsync(
                authResponse.Token,
                new List<long> { shipmentId },
                cancellationToken);

            _logger.LogInformation(
                "AWB generated successfully: {AwbCode} with courier {CourierName}",
                awbData.AwbCode, awbData.CourierName);

            return new AwbGenerationResult
            {
                Success = true,
                AwbCode = awbData.AwbCode,
                CourierName = awbData.CourierName,
                CourierCompanyId = awbData.CourierCompanyId,
                LabelUrl = labelResponse?.LabelUrl,
                TrackingUrl = $"https://shiprocket.co/tracking/{awbData.AwbCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AWB for shipment {ShipmentId}", shipmentId);
            return new AwbGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error generating AWB: {ex.Message}"
            };
        }
    }
}
