using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Query to get wallet balance for a courier account.
/// </summary>
public record GetCourierWalletBalanceQuery : IRequest<Result<CourierWalletBalanceDto>>, ITenantRequest
{
    public Guid CourierAccountId { get; init; }
}

/// <summary>
/// Wallet balance information.
/// </summary>
public record CourierWalletBalanceDto
{
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "INR";
    public DateTime? LastUpdated { get; init; }
    public string? AccountEmail { get; init; }
}

public class GetCourierWalletBalanceQueryHandler : IRequestHandler<GetCourierWalletBalanceQuery, Result<CourierWalletBalanceDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICourierWalletService _walletService;
    private readonly ILogger<GetCourierWalletBalanceQueryHandler> _logger;

    public GetCourierWalletBalanceQueryHandler(
        ITenantDbContext dbContext,
        ICourierWalletService walletService,
        ILogger<GetCourierWalletBalanceQueryHandler> logger)
    {
        _dbContext = dbContext;
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<Result<CourierWalletBalanceDto>> Handle(
        GetCourierWalletBalanceQuery request,
        CancellationToken cancellationToken)
    {
        // Get courier account
        var courierAccount = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.CourierAccountId && c.DeletedAt == null, cancellationToken);

        if (courierAccount == null)
        {
            return Result<CourierWalletBalanceDto>.Failure("Courier account not found");
        }

        if (!courierAccount.IsActive)
        {
            return Result<CourierWalletBalanceDto>.Failure("Courier account is not active");
        }

        if (!courierAccount.IsConnected)
        {
            return Result<CourierWalletBalanceDto>.Failure("Courier account is not connected. Please check your credentials.");
        }

        try
        {
            _logger.LogInformation("Fetching wallet balance for courier account {CourierAccountId}", request.CourierAccountId);

            var balance = await _walletService.GetWalletBalanceAsync(
                courierAccount.CourierType,
                courierAccount.ApiKey ?? "",
                courierAccount.ApiSecret ?? "",
                cancellationToken);

            if (balance == null)
            {
                return Result<CourierWalletBalanceDto>.Failure("Failed to retrieve wallet balance");
            }

            var dto = new CourierWalletBalanceDto
            {
                Balance = balance.Value,
                Currency = "INR",
                LastUpdated = DateTime.UtcNow,
                AccountEmail = courierAccount.ApiKey
            };

            _logger.LogInformation("Wallet balance retrieved: {Balance} {Currency} for account {Email}",
                dto.Balance, dto.Currency, courierAccount.ApiKey);

            return Result<CourierWalletBalanceDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching wallet balance for courier account {CourierAccountId}", request.CourierAccountId);
            return Result<CourierWalletBalanceDto>.Failure($"Error fetching wallet balance: {ex.Message}");
        }
    }
}
