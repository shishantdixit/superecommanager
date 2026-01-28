using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using System.Text.Json;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Command to update courier account details.
/// </summary>
[RequirePermission("couriers.update")]
[RequireFeature("courier_management")]
public record UpdateCourierAccountCommand : IRequest<Result<CourierAccountDto>>, ITenantRequest
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string? AccountId { get; init; }
    public string? ChannelId { get; init; }
    public string? PickupLocation { get; init; }
    public bool? IsActive { get; init; }
}

public class UpdateCourierAccountCommandHandler : IRequestHandler<UpdateCourierAccountCommand, Result<CourierAccountDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<UpdateCourierAccountCommandHandler> _logger;

    public UpdateCourierAccountCommandHandler(
        ITenantDbContext dbContext,
        ILogger<UpdateCourierAccountCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CourierAccountDto>> Handle(
        UpdateCourierAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.DeletedAt == null, cancellationToken);

        if (account == null)
        {
            return Result<CourierAccountDto>.Failure("Courier account not found");
        }

        // Update name if provided
        if (!string.IsNullOrEmpty(request.Name))
        {
            // Check name uniqueness
            var nameExists = await _dbContext.CourierAccounts
                .AnyAsync(c => c.Name == request.Name && c.Id != request.Id && c.DeletedAt == null,
                    cancellationToken);

            if (nameExists)
            {
                return Result<CourierAccountDto>.Failure("A courier account with this name already exists");
            }

            account.UpdateName(request.Name);
        }

        // Update credentials if provided
        if (request.ApiKey != null || request.ApiSecret != null)
        {
            account.SetCredentials(
                request.ApiKey,
                request.ApiSecret,
                null,
                request.AccountId,
                request.ChannelId);
        }

        // Update pickup location in settings if provided
        if (request.PickupLocation != null)
        {
            var settings = new Dictionary<string, object>();

            // Parse existing settings if any
            if (!string.IsNullOrEmpty(account.SettingsJson))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<Dictionary<string, object>>(account.SettingsJson)
                        ?? new Dictionary<string, object>();
                }
                catch
                {
                    // Ignore parsing errors and start fresh
                }
            }

            settings["pickupLocation"] = request.PickupLocation;
            account.UpdateSettings(JsonSerializer.Serialize(settings));
        }

        // Update active status if provided
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
            {
                account.Activate();
            }
            else
            {
                account.Deactivate();
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated courier account {AccountId}", account.Id);

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
