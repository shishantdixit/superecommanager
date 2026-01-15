using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Notifications;

/// <summary>
/// Query to get notification logs with filtering.
/// </summary>
[RequirePermission("notifications.logs.view")]
[RequireFeature("notifications")]
public record GetNotificationLogsQuery : IRequest<Result<PaginatedResult<NotificationLogListDto>>>, ITenantRequest
{
    public NotificationLogFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public class GetNotificationLogsQueryHandler : IRequestHandler<GetNotificationLogsQuery, Result<PaginatedResult<NotificationLogListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetNotificationLogsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<NotificationLogListDto>>> Handle(
        GetNotificationLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.NotificationLogs.AsNoTracking();

        // Apply filters
        if (request.Filter != null)
        {
            var filter = request.Filter;

            if (filter.Type.HasValue)
                query = query.Where(l => l.Type == filter.Type.Value);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(l => l.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.Recipient))
                query = query.Where(l => l.Recipient.Contains(filter.Recipient));

            if (!string.IsNullOrWhiteSpace(filter.ReferenceType))
                query = query.Where(l => l.ReferenceType == filter.ReferenceType);

            if (!string.IsNullOrWhiteSpace(filter.ReferenceId))
                query = query.Where(l => l.ReferenceId == filter.ReferenceId);

            if (filter.FromDate.HasValue)
                query = query.Where(l => l.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(l => l.CreatedAt <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = logs.Select(l => new NotificationLogListDto
        {
            Id = l.Id,
            Type = l.Type,
            Recipient = l.Recipient,
            Subject = l.Subject,
            Status = l.Status,
            ReferenceType = l.ReferenceType,
            ReferenceId = l.ReferenceId,
            CreatedAt = l.CreatedAt,
            SentAt = l.SentAt,
            DeliveredAt = l.DeliveredAt,
            FailedAt = l.FailedAt
        }).ToList();

        var result = new PaginatedResult<NotificationLogListDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);

        return Result<PaginatedResult<NotificationLogListDto>>.Success(result);
    }
}

/// <summary>
/// Query to get a specific notification log by ID.
/// </summary>
[RequirePermission("notifications.logs.view")]
[RequireFeature("notifications")]
public record GetNotificationLogByIdQuery : IRequest<Result<NotificationLogDetailDto>>, ITenantRequest
{
    public Guid LogId { get; init; }
}

public class GetNotificationLogByIdQueryHandler : IRequestHandler<GetNotificationLogByIdQuery, Result<NotificationLogDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetNotificationLogByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<NotificationLogDetailDto>> Handle(
        GetNotificationLogByIdQuery request,
        CancellationToken cancellationToken)
    {
        var log = await _dbContext.NotificationLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LogId, cancellationToken);

        if (log == null)
        {
            return Result<NotificationLogDetailDto>.Failure("Notification log not found");
        }

        // Get template name if applicable
        string? templateName = null;
        if (log.TemplateId.HasValue)
        {
            var template = await _dbContext.NotificationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == log.TemplateId.Value, cancellationToken);
            templateName = template?.Name;
        }

        var dto = new NotificationLogDetailDto
        {
            Id = log.Id,
            Type = log.Type,
            Recipient = log.Recipient,
            Subject = log.Subject,
            Content = log.Content,
            Status = log.Status,
            ProviderResponse = log.ProviderResponse,
            ProviderMessageId = log.ProviderMessageId,
            TemplateId = log.TemplateId,
            TemplateName = templateName,
            ReferenceType = log.ReferenceType,
            ReferenceId = log.ReferenceId,
            CreatedAt = log.CreatedAt,
            SentAt = log.SentAt,
            DeliveredAt = log.DeliveredAt,
            FailedAt = log.FailedAt,
            FailureReason = log.FailureReason
        };

        return Result<NotificationLogDetailDto>.Success(dto);
    }
}
