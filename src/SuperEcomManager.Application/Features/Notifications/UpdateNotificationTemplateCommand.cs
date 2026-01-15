using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Notifications;

/// <summary>
/// Command to update an existing notification template.
/// </summary>
[RequirePermission("notifications.templates.update")]
[RequireFeature("notifications")]
public record UpdateNotificationTemplateCommand : IRequest<Result<NotificationTemplateDetailDto>>, ITenantRequest
{
    public Guid TemplateId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Body { get; init; } = string.Empty;
}

public class UpdateNotificationTemplateCommandHandler : IRequestHandler<UpdateNotificationTemplateCommand, Result<NotificationTemplateDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<UpdateNotificationTemplateCommandHandler> _logger;

    public UpdateNotificationTemplateCommandHandler(
        ITenantDbContext dbContext,
        ILogger<UpdateNotificationTemplateCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<NotificationTemplateDetailDto>> Handle(
        UpdateNotificationTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _dbContext.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template == null)
        {
            return Result<NotificationTemplateDetailDto>.Failure("Template not found");
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
        if (template.Type == NotificationType.Email && string.IsNullOrWhiteSpace(request.Subject))
        {
            return Result<NotificationTemplateDetailDto>.Failure("Subject is required for email templates");
        }

        try
        {
            template.Update(request.Name, request.Subject, request.Body);
        }
        catch (InvalidOperationException ex)
        {
            return Result<NotificationTemplateDetailDto>.Failure(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification template updated: {Code} ({Id})",
            template.Code, template.Id);

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

/// <summary>
/// Command to toggle template active status.
/// </summary>
[RequirePermission("notifications.templates.update")]
[RequireFeature("notifications")]
public record ToggleNotificationTemplateCommand : IRequest<Result<NotificationTemplateListDto>>, ITenantRequest
{
    public Guid TemplateId { get; init; }
    public bool IsActive { get; init; }
}

public class ToggleNotificationTemplateCommandHandler : IRequestHandler<ToggleNotificationTemplateCommand, Result<NotificationTemplateListDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<ToggleNotificationTemplateCommandHandler> _logger;

    public ToggleNotificationTemplateCommandHandler(
        ITenantDbContext dbContext,
        ILogger<ToggleNotificationTemplateCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<NotificationTemplateListDto>> Handle(
        ToggleNotificationTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _dbContext.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template == null)
        {
            return Result<NotificationTemplateListDto>.Failure("Template not found");
        }

        // Use reflection to set IsActive since there's no public setter
        var isActiveProperty = typeof(Domain.Entities.Notifications.NotificationTemplate)
            .GetProperty(nameof(Domain.Entities.Notifications.NotificationTemplate.IsActive));
        isActiveProperty?.SetValue(template, request.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification template {Action}: {Code} ({Id})",
            request.IsActive ? "activated" : "deactivated",
            template.Code, template.Id);

        var dto = new NotificationTemplateListDto
        {
            Id = template.Id,
            Code = template.Code,
            Name = template.Name,
            Type = template.Type,
            IsActive = template.IsActive,
            IsSystem = template.IsSystem,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };

        return Result<NotificationTemplateListDto>.Success(dto);
    }
}
