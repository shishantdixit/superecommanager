using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers.Shiprocket;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Service for courier wallet operations.
/// </summary>
public class CourierWalletService : ICourierWalletService
{
    private readonly IShiprocketClient _shiprocketClient;
    private readonly ILogger<CourierWalletService> _logger;

    public CourierWalletService(
        IShiprocketClient shiprocketClient,
        ILogger<CourierWalletService> logger)
    {
        _shiprocketClient = shiprocketClient;
        _logger = logger;
    }

    public async Task<decimal?> GetWalletBalanceAsync(
        CourierType courierType,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default)
    {
        try
        {
            switch (courierType)
            {
                case CourierType.Shiprocket:
                    return await GetShiprocketBalanceAsync(apiKey, apiSecret, cancellationToken);

                default:
                    _logger.LogWarning("Wallet balance not supported for courier type {CourierType}", courierType);
                    return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching wallet balance for {CourierType}", courierType);
            throw;
        }
    }

    /// <summary>
    /// Credentials must be from a Shiprocket API user (Settings → API → API Users),
    /// not a regular account user. Regular users trigger OTP authentication.
    /// </summary>
    private async Task<decimal?> GetShiprocketBalanceAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching Shiprocket wallet balance for {Email}", email);

        // Authenticate (requires API user credentials, not regular user)
        var authResponse = await _shiprocketClient.AuthenticateAsync(email, password, cancellationToken);

        if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
        {
            _logger.LogError("Failed to authenticate with Shiprocket for {Email}", email);
            return null;
        }

        // Get wallet balance
        var walletResponse = await _shiprocketClient.GetWalletBalanceAsync(authResponse.Token, cancellationToken);

        if (walletResponse?.Data == null)
        {
            _logger.LogError("Failed to retrieve wallet balance from Shiprocket for {Email}", email);
            return null;
        }

        _logger.LogInformation("Shiprocket wallet balance: {Balance} for {Email}",
            walletResponse.Data.Balance, email);

        return walletResponse.Data.Balance;
    }
}
