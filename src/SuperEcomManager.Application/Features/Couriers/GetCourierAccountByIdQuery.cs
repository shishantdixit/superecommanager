using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using System.Text.Json;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Query to get a courier account by ID.
/// </summary>
public record GetCourierAccountByIdQuery : IRequest<Result<CourierAccountDetailDto>>, ITenantRequest
{
    public Guid Id { get; init; }
}

public class GetCourierAccountByIdQueryHandler : IRequestHandler<GetCourierAccountByIdQuery, Result<CourierAccountDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetCourierAccountByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CourierAccountDetailDto>> Handle(
        GetCourierAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.DeletedAt == null, cancellationToken);

        if (account == null)
        {
            return Result<CourierAccountDetailDto>.Failure("Courier account not found");
        }

        // Parse settings JSON
        Dictionary<string, object>? settings = null;
        if (!string.IsNullOrEmpty(account.SettingsJson))
        {
            try
            {
                settings = JsonSerializer.Deserialize<Dictionary<string, object>>(account.SettingsJson);
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        var dto = new CourierAccountDetailDto
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
            CreatedAt = account.CreatedAt,
            HasApiKey = !string.IsNullOrEmpty(account.ApiKey),
            HasApiSecret = !string.IsNullOrEmpty(account.ApiSecret),
            HasAccessToken = !string.IsNullOrEmpty(account.AccessToken),
            AccountId = account.AccountId,
            ChannelId = account.ChannelId,
            WebhookUrl = account.WebhookUrl,
            Settings = settings
        };

        return Result<CourierAccountDetailDto>.Success(dto);
    }
}
