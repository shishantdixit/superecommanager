using System.Security.Cryptography;
using MediatR;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Webhooks;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Webhooks;

/// <summary>
/// Command to create a new webhook subscription.
/// </summary>
[RequirePermission("webhooks.manage")]
[RequireFeature("webhooks")]
public record CreateWebhookSubscriptionCommand : IRequest<Result<WebhookSubscriptionDto>>, ITenantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public List<WebhookEvent> Events { get; init; } = new();
    public Dictionary<string, string>? Headers { get; init; }
    public int MaxRetries { get; init; } = 3;
    public int TimeoutSeconds { get; init; } = 30;
}

public class CreateWebhookSubscriptionCommandHandler
    : IRequestHandler<CreateWebhookSubscriptionCommand, Result<WebhookSubscriptionDto>>
{
    private readonly ITenantDbContext _dbContext;

    public CreateWebhookSubscriptionCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WebhookSubscriptionDto>> Handle(
        CreateWebhookSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        // Generate a secure webhook secret
        var secret = GenerateSecret();

        try
        {
            var subscription = WebhookSubscription.Create(
                request.Name,
                request.Url,
                secret,
                request.Events,
                request.Headers,
                request.MaxRetries,
                request.TimeoutSeconds);

            _dbContext.WebhookSubscriptions.Add(subscription);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var dto = new WebhookSubscriptionDto
            {
                Id = subscription.Id,
                Name = subscription.Name,
                Url = subscription.Url,
                IsActive = subscription.IsActive,
                Events = subscription.Events,
                Headers = subscription.Headers,
                MaxRetries = subscription.MaxRetries,
                TimeoutSeconds = subscription.TimeoutSeconds,
                TotalDeliveries = subscription.TotalDeliveries,
                SuccessfulDeliveries = subscription.SuccessfulDeliveries,
                FailedDeliveries = subscription.FailedDeliveries,
                CreatedAt = subscription.CreatedAt
            };

            return Result<WebhookSubscriptionDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            return Result<WebhookSubscriptionDto>.Failure(ex.Message);
        }
    }

    private static string GenerateSecret()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
