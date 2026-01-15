using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Notifications;

/// <summary>
/// Query to get notification templates with filtering.
/// </summary>
[RequirePermission("notifications.templates.view")]
[RequireFeature("notifications")]
public record GetNotificationTemplatesQuery : IRequest<Result<PaginatedResult<NotificationTemplateListDto>>>, ITenantRequest
{
    public NotificationTemplateFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetNotificationTemplatesQueryHandler : IRequestHandler<GetNotificationTemplatesQuery, Result<PaginatedResult<NotificationTemplateListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetNotificationTemplatesQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<NotificationTemplateListDto>>> Handle(
        GetNotificationTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.NotificationTemplates.AsNoTracking();

        // Apply filters
        if (request.Filter != null)
        {
            var filter = request.Filter;

            if (filter.Type.HasValue)
                query = query.Where(t => t.Type == filter.Type.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(t => t.IsActive == filter.IsActive.Value);

            if (filter.IsSystem.HasValue)
                query = query.Where(t => t.IsSystem == filter.IsSystem.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(t =>
                    t.Code.ToLower().Contains(searchTerm) ||
                    t.Name.ToLower().Contains(searchTerm));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var templates = await query
            .OrderBy(t => t.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = templates.Select(t => new NotificationTemplateListDto
        {
            Id = t.Id,
            Code = t.Code,
            Name = t.Name,
            Type = t.Type,
            IsActive = t.IsActive,
            IsSystem = t.IsSystem,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();

        var result = new PaginatedResult<NotificationTemplateListDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);

        return Result<PaginatedResult<NotificationTemplateListDto>>.Success(result);
    }
}

/// <summary>
/// Query to get a specific notification template by ID.
/// </summary>
[RequirePermission("notifications.templates.view")]
[RequireFeature("notifications")]
public record GetNotificationTemplateByIdQuery : IRequest<Result<NotificationTemplateDetailDto>>, ITenantRequest
{
    public Guid TemplateId { get; init; }
}

public class GetNotificationTemplateByIdQueryHandler : IRequestHandler<GetNotificationTemplateByIdQuery, Result<NotificationTemplateDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetNotificationTemplateByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<NotificationTemplateDetailDto>> Handle(
        GetNotificationTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _dbContext.NotificationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template == null)
        {
            return Result<NotificationTemplateDetailDto>.Failure("Template not found");
        }

        // Parse variables from JSON
        var variables = new List<string>();
        if (!string.IsNullOrWhiteSpace(template.Variables))
        {
            try
            {
                variables = JsonSerializer.Deserialize<List<string>>(template.Variables) ?? new List<string>();
            }
            catch
            {
                // Ignore JSON parse errors
            }
        }

        var dto = new NotificationTemplateDetailDto
        {
            Id = template.Id,
            Code = template.Code,
            Name = template.Name,
            Type = template.Type,
            Subject = template.Subject,
            Body = template.Body,
            Variables = variables,
            IsActive = template.IsActive,
            IsSystem = template.IsSystem,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };

        return Result<NotificationTemplateDetailDto>.Success(dto);
    }
}
