using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Notifications;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Notifications;

/// <summary>
/// Command to create a new notification template.
/// </summary>
[RequirePermission("notifications.templates.create")]
[RequireFeature("notifications")]
public record CreateNotificationTemplateCommand : IRequest<Result<NotificationTemplateDetailDto>>, ITenantRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public string? Subject { get; init; }
    public string Body { get; init; } = string.Empty;
    public List<string>? Variables { get; init; }
}

public class CreateNotificationTemplateCommandHandler : IRequestHandler<CreateNotificationTemplateCommand, Result<NotificationTemplateDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<CreateNotificationTemplateCommandHandler> _logger;

    public CreateNotificationTemplateCommandHandler(
        ITenantDbContext dbContext,
        ILogger<CreateNotificationTemplateCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<NotificationTemplateDetailDto>> Handle(
        CreateNotificationTemplateCommand request,
        CancellationToken cancellationToken)
    {
        // Validate code
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Result<NotificationTemplateDetailDto>.Failure("Template code is required");
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<NotificationTemplateDetailDto>.Failure("Template name is required");
        }

        // Validate body
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Result<NotificationTemplateDetailDto>.Failure("Template body is required");
        }

        // Check for email subject
        if (request.Type == NotificationType.Email && string.IsNullOrWhiteSpace(request.Subject))
        {
            return Result<NotificationTemplateDetailDto>.Failure("Subject is required for email templates");
        }

        // Check if code already exists
        var existingTemplate = await _dbContext.NotificationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == request.Code, cancellationToken);

        if (existingTemplate != null)
        {
            return Result<NotificationTemplateDetailDto>.Failure($"Template with code '{request.Code}' already exists");
        }

        // Create the template
        var template = NotificationTemplate.Create(
            request.Code,
            request.Name,
            request.Type,
            request.Body,
            request.Subject);

        _dbContext.NotificationTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification template created: {Code} ({Type})",
            request.Code, request.Type);

        var dto = new NotificationTemplateDetailDto
        {
            Id = template.Id,
            Code = template.Code,
            Name = template.Name,
            Type = template.Type,
            Subject = template.Subject,
            Body = template.Body,
            Variables = request.Variables ?? new List<string>(),
            IsActive = template.IsActive,
            IsSystem = template.IsSystem,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };

        return Result<NotificationTemplateDetailDto>.Success(dto);
    }
}
