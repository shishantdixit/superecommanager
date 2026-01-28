using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for courier wallet operations.
/// </summary>
public interface ICourierWalletService
{
    /// <summary>
    /// Gets the wallet balance for a courier account.
    /// </summary>
    Task<decimal?> GetWalletBalanceAsync(
        CourierType courierType,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default);
}
