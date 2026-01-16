using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Webhooks;

/// <summary>
/// Query to get all webhook subscriptions.
/// </summary>
[RequirePermission("webhooks.view")]
[RequireFeature("webhooks")]
public record GetWebhookSubscriptionsQuery : IRequest<Result<List<WebhookSubscriptionListDto>>>, ITenantRequest
{
    public bool? IsActive { get; init; }
}

public class GetWebhookSubscriptionsQueryHandler
    : IRequestHandler<GetWebhookSubscriptionsQuery, Result<List<WebhookSubscriptionListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetWebhookSubscriptionsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<WebhookSubscriptionListDto>>> Handle(
        GetWebhookSubscriptionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.WebhookSubscriptions.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(w => w.IsActive == request.IsActive.Value);

        var subscriptions = await query
            .OrderBy(w => w.Name)
            .Select(w => new WebhookSubscriptionListDto
            {
                Id = w.Id,
                Name = w.Name,
                Url = w.Url,
                IsActive = w.IsActive,
                EventCount = w.Events.Count,
                LastTriggeredAt = w.LastTriggeredAt,
                TotalDeliveries = w.TotalDeliveries,
                SuccessRate = w.TotalDeliveries > 0
                    ? (double)w.SuccessfulDeliveries / w.TotalDeliveries * 100
                    : 0
            })
            .ToListAsync(cancellationToken);

        return Result<List<WebhookSubscriptionListDto>>.Success(subscriptions);
    }
}

/// <summary>
/// Query to get a specific webhook subscription.
/// </summary>
[RequirePermission("webhooks.view")]
[RequireFeature("webhooks")]
public record GetWebhookSubscriptionQuery : IRequest<Result<WebhookSubscriptionDto>>, ITenantRequest
{
    public Guid Id { get; init; }
}

public class GetWebhookSubscriptionQueryHandler
    : IRequestHandler<GetWebhookSubscriptionQuery, Result<WebhookSubscriptionDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetWebhookSubscriptionQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WebhookSubscriptionDto>> Handle(
        GetWebhookSubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (subscription == null)
            return Result<WebhookSubscriptionDto>.Failure("Webhook subscription not found.");

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
}

/// <summary>
/// Query to get webhook deliveries.
/// </summary>
[RequirePermission("webhooks.view")]
[RequireFeature("webhooks")]
public record GetWebhookDeliveriesQuery : IRequest<PaginatedResult<WebhookDeliveryDto>>, ITenantRequest
{
    public Guid? SubscriptionId { get; init; }
    public WebhookDeliveryStatus? Status { get; init; }
    public WebhookEvent? Event { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetWebhookDeliveriesQueryHandler
    : IRequestHandler<GetWebhookDeliveriesQuery, PaginatedResult<WebhookDeliveryDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetWebhookDeliveriesQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResult<WebhookDeliveryDto>> Handle(
        GetWebhookDeliveriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.WebhookDeliveries
            .AsNoTracking()
            .Include(d => d.Subscription)
            .AsQueryable();

        if (request.SubscriptionId.HasValue)
            query = query.Where(d => d.WebhookSubscriptionId == request.SubscriptionId.Value);

        if (request.Status.HasValue)
            query = query.Where(d => d.Status == request.Status.Value);

        if (request.Event.HasValue)
            query = query.Where(d => d.Event == request.Event.Value);

        if (request.FromDate.HasValue)
            query = query.Where(d => d.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(d => d.CreatedAt <= request.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var deliveries = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new WebhookDeliveryDto
            {
                Id = d.Id,
                WebhookSubscriptionId = d.WebhookSubscriptionId,
                WebhookName = d.Subscription != null ? d.Subscription.Name : "",
                Event = d.Event,
                Status = d.Status,
                AttemptCount = d.AttemptCount,
                HttpStatusCode = d.HttpStatusCode,
                ErrorMessage = d.ErrorMessage,
                CreatedAt = d.CreatedAt,
                DeliveredAt = d.DeliveredAt,
                NextRetryAt = d.NextRetryAt,
                Duration = d.Duration
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<WebhookDeliveryDto>(
            deliveries, totalCount, request.Page, request.PageSize);
    }
}

/// <summary>
/// Query to get webhook delivery details.
/// </summary>
[RequirePermission("webhooks.view")]
[RequireFeature("webhooks")]
public record GetWebhookDeliveryDetailQuery : IRequest<Result<WebhookDeliveryDetailDto>>, ITenantRequest
{
    public Guid Id { get; init; }
}

public class GetWebhookDeliveryDetailQueryHandler
    : IRequestHandler<GetWebhookDeliveryDetailQuery, Result<WebhookDeliveryDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetWebhookDeliveryDetailQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WebhookDeliveryDetailDto>> Handle(
        GetWebhookDeliveryDetailQuery request,
        CancellationToken cancellationToken)
    {
        var delivery = await _dbContext.WebhookDeliveries
            .AsNoTracking()
            .Include(d => d.Subscription)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (delivery == null)
            return Result<WebhookDeliveryDetailDto>.Failure("Webhook delivery not found.");

        var dto = new WebhookDeliveryDetailDto
        {
            Id = delivery.Id,
            WebhookSubscriptionId = delivery.WebhookSubscriptionId,
            WebhookName = delivery.Subscription?.Name ?? "",
            Event = delivery.Event,
            Status = delivery.Status,
            AttemptCount = delivery.AttemptCount,
            HttpStatusCode = delivery.HttpStatusCode,
            ErrorMessage = delivery.ErrorMessage,
            CreatedAt = delivery.CreatedAt,
            DeliveredAt = delivery.DeliveredAt,
            NextRetryAt = delivery.NextRetryAt,
            Duration = delivery.Duration,
            Payload = delivery.Payload,
            ResponseBody = delivery.ResponseBody
        };

        return Result<WebhookDeliveryDetailDto>.Success(dto);
    }
}

/// <summary>
/// Query to get webhook statistics.
/// </summary>
[RequirePermission("webhooks.view")]
[RequireFeature("webhooks")]
public record GetWebhookStatsQuery : IRequest<Result<WebhookStatsDto>>, ITenantRequest
{
    public int Days { get; init; } = 30;
}

public class GetWebhookStatsQueryHandler
    : IRequestHandler<GetWebhookStatsQuery, Result<WebhookStatsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetWebhookStatsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WebhookStatsDto>> Handle(
        GetWebhookStatsQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = DateTime.UtcNow.AddDays(-request.Days);

        var subscriptionStats = await _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalSubscriptions = g.Count(),
                ActiveSubscriptions = g.Count(s => s.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var deliveryStats = await _dbContext.WebhookDeliveries
            .AsNoTracking()
            .Where(d => d.CreatedAt >= fromDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalDeliveries = g.Count(),
                SuccessfulDeliveries = g.Count(d => d.Status == WebhookDeliveryStatus.Delivered),
                FailedDeliveries = g.Count(d => d.Status == WebhookDeliveryStatus.Failed),
                PendingDeliveries = g.Count(d => d.Status == WebhookDeliveryStatus.Pending || d.Status == WebhookDeliveryStatus.Retrying)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var eventStats = await _dbContext.WebhookDeliveries
            .AsNoTracking()
            .Where(d => d.CreatedAt >= fromDate)
            .GroupBy(d => d.Event)
            .Select(g => new WebhookEventStatsDto
            {
                Event = g.Key,
                TotalDeliveries = g.Count(),
                SuccessfulDeliveries = g.Count(d => d.Status == WebhookDeliveryStatus.Delivered),
                FailedDeliveries = g.Count(d => d.Status == WebhookDeliveryStatus.Failed),
                SuccessRate = g.Count() > 0
                    ? (double)g.Count(d => d.Status == WebhookDeliveryStatus.Delivered) / g.Count() * 100
                    : 0
            })
            .OrderByDescending(e => e.TotalDeliveries)
            .ToListAsync(cancellationToken);

        var totalDeliveries = deliveryStats?.TotalDeliveries ?? 0;
        var successfulDeliveries = deliveryStats?.SuccessfulDeliveries ?? 0;

        var stats = new WebhookStatsDto
        {
            TotalSubscriptions = subscriptionStats?.TotalSubscriptions ?? 0,
            ActiveSubscriptions = subscriptionStats?.ActiveSubscriptions ?? 0,
            TotalDeliveries = totalDeliveries,
            SuccessfulDeliveries = successfulDeliveries,
            FailedDeliveries = deliveryStats?.FailedDeliveries ?? 0,
            PendingDeliveries = deliveryStats?.PendingDeliveries ?? 0,
            OverallSuccessRate = totalDeliveries > 0
                ? (double)successfulDeliveries / totalDeliveries * 100
                : 0,
            EventStats = eventStats
        };

        return Result<WebhookStatsDto>.Success(stats);
    }
}

/// <summary>
/// Query to get list of available webhook events.
/// </summary>
[RequirePermission("webhooks.view")]
[RequireFeature("webhooks")]
public record GetWebhookEventsQuery : IRequest<Result<List<string>>>, ITenantRequest;

public class GetWebhookEventsQueryHandler
    : IRequestHandler<GetWebhookEventsQuery, Result<List<string>>>
{
    public Task<Result<List<string>>> Handle(
        GetWebhookEventsQuery request,
        CancellationToken cancellationToken)
    {
        var events = Enum.GetNames<WebhookEvent>().ToList();
        return Task.FromResult(Result<List<string>>.Success(events));
    }
}
