using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Webhooks;

/// <summary>
/// Command to update an existing webhook subscription.
/// </summary>
[RequirePermission("webhooks.manage")]
[RequireFeature("webhooks")]
public record UpdateWebhookSubscriptionCommand : IRequest<Result<WebhookSubscriptionDto>>, ITenantRequest
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Url { get; init; }
    public List<WebhookEvent>? Events { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
    public int? MaxRetries { get; init; }
    public int? TimeoutSeconds { get; init; }
}

public class UpdateWebhookSubscriptionCommandHandler
    : IRequestHandler<UpdateWebhookSubscriptionCommand, Result<WebhookSubscriptionDto>>
{
    private readonly ITenantDbContext _dbContext;

    public UpdateWebhookSubscriptionCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WebhookSubscriptionDto>> Handle(
        UpdateWebhookSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (subscription == null)
            return Result<WebhookSubscriptionDto>.Failure("Webhook subscription not found.");

        try
        {
            subscription.Update(
                request.Name ?? subscription.Name,
                request.Url ?? subscription.Url,
                request.Events ?? subscription.Events,
                request.Headers,
                request.MaxRetries,
                request.TimeoutSeconds);

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
                LastTriggeredAt = subscription.LastTriggeredAt,
                TotalDeliveries = subscription.TotalDeliveries,
                SuccessfulDeliveries = subscription.SuccessfulDeliveries,
                FailedDeliveries = subscription.FailedDeliveries,
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt
            };

            return Result<WebhookSubscriptionDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            return Result<WebhookSubscriptionDto>.Failure(ex.Message);
        }
    }
}

/// <summary>
/// Command to toggle webhook subscription active status.
/// </summary>
[RequirePermission("webhooks.manage")]
[RequireFeature("webhooks")]
public record ToggleWebhookSubscriptionCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid Id { get; init; }
    public bool IsActive { get; init; }
}

public class ToggleWebhookSubscriptionCommandHandler
    : IRequestHandler<ToggleWebhookSubscriptionCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;

    public ToggleWebhookSubscriptionCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> Handle(
        ToggleWebhookSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (subscription == null)
            return Result<bool>.Failure("Webhook subscription not found.");

        if (request.IsActive)
            subscription.Activate();
        else
            subscription.Deactivate();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(subscription.IsActive);
    }
}

/// <summary>
/// Command to delete a webhook subscription.
/// </summary>
[RequirePermission("webhooks.manage")]
[RequireFeature("webhooks")]
public record DeleteWebhookSubscriptionCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid Id { get; init; }
}

public class DeleteWebhookSubscriptionCommandHandler
    : IRequestHandler<DeleteWebhookSubscriptionCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;

    public DeleteWebhookSubscriptionCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> Handle(
        DeleteWebhookSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (subscription == null)
            return Result<bool>.Failure("Webhook subscription not found.");

        _dbContext.WebhookSubscriptions.Remove(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

/// <summary>
/// Command to regenerate webhook secret.
/// </summary>
[RequirePermission("webhooks.manage")]
[RequireFeature("webhooks")]
public record RegenerateWebhookSecretCommand : IRequest<Result<string>>, ITenantRequest
{
    public Guid Id { get; init; }
}

public class RegenerateWebhookSecretCommandHandler
    : IRequestHandler<RegenerateWebhookSecretCommand, Result<string>>
{
    private readonly ITenantDbContext _dbContext;

    public RegenerateWebhookSecretCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<string>> Handle(
        RegenerateWebhookSecretCommand request,
        CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (subscription == null)
            return Result<string>.Failure("Webhook subscription not found.");

        var newSecret = GenerateSecret();
        subscription.UpdateSecret(newSecret);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(newSecret);
    }

    private static string GenerateSecret()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
