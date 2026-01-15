using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Command to update courier account credentials.
/// </summary>
[RequirePermission("couriers.update")]
[RequireFeature("courier_management")]
public record UpdateCourierCredentialsCommand : IRequest<Result<CourierAccountDto>>, ITenantRequest
{
    public Guid AccountId { get; init; }
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string? AccessToken { get; init; }
    public string? AccountId_ { get; init; }
    public string? ChannelId { get; init; }
}

public class UpdateCourierCredentialsCommandHandler : IRequestHandler<UpdateCourierCredentialsCommand, Result<CourierAccountDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<UpdateCourierCredentialsCommandHandler> _logger;

    // Delegate for validating credentials (injected from Integrations layer)
    public Func<Guid, CancellationToken, Task<bool>>? ValidateCredentials { get; set; }

    public UpdateCourierCredentialsCommandHandler(
        ITenantDbContext dbContext,
        ILogger<UpdateCourierCredentialsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CourierAccountDto>> Handle(
        UpdateCourierCredentialsCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.AccountId, cancellationToken);

        if (account == null)
        {
            return Result<CourierAccountDto>.Failure("Courier account not found");
        }

        // Update credentials
        account.SetCredentials(
            request.ApiKey,
            request.ApiSecret,
            request.AccessToken,
            request.AccountId_,
            request.ChannelId);

        // Validate credentials if validator is available
        if (ValidateCredentials != null)
        {
            try
            {
                var isValid = await ValidateCredentials(account.Id, cancellationToken);
                if (isValid)
                {
                    account.MarkConnected();
                }
                else
                {
                    account.MarkDisconnected("Invalid credentials");
                }
            }
            catch (Exception ex)
            {
                account.MarkDisconnected(ex.Message);
                _logger.LogWarning(ex, "Failed to validate credentials for account {AccountId}", account.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated credentials for courier account {AccountId}", account.Id);

        return Result<CourierAccountDto>.Success(new CourierAccountDto
        {
            Id = account.Id,
            Name = account.Name,
            CourierType = account.CourierType,
            IsActive = account.IsActive,
            IsDefault = account.IsDefault,
            IsConnected = account.IsConnected,
            LastConnectedAt = account.LastConnectedAt,
            LastError = account.LastError,
            Priority = account.Priority,
            SupportsCOD = account.SupportsCOD,
            SupportsReverse = account.SupportsReverse,
            SupportsExpress = account.SupportsExpress,
            CreatedAt = account.CreatedAt
        });
    }
}
