using System.Text.RegularExpressions;
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
/// Command to send a notification.
/// </summary>
[RequirePermission("notifications.send")]
[RequireFeature("notifications")]
public record SendNotificationCommand : IRequest<Result<SendNotificationResultDto>>, ITenantRequest
{
    public NotificationType Type { get; init; }
    public string Recipient { get; init; } = string.Empty;
    public string? TemplateCode { get; init; }
    public string? Subject { get; init; }
    public string? Content { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
}

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Result<SendNotificationResultDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        ITenantDbContext dbContext,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<SendNotificationResultDto>> Handle(
        SendNotificationCommand request,
        CancellationToken cancellationToken)
    {
        // Validate recipient
        if (string.IsNullOrWhiteSpace(request.Recipient))
        {
            return Result<SendNotificationResultDto>.Failure("Recipient is required");
        }

        // Validate recipient format based on type
        if (!ValidateRecipient(request.Type, request.Recipient, out var validationError))
        {
            return Result<SendNotificationResultDto>.Failure(validationError);
        }

        string? subject = request.Subject;
        string content;
        Guid? templateId = null;

        // If template code is provided, use the template
        if (!string.IsNullOrWhiteSpace(request.TemplateCode))
        {
            var template = await _dbContext.NotificationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == request.TemplateCode && t.IsActive, cancellationToken);

            if (template == null)
            {
                return Result<SendNotificationResultDto>.Failure($"Template '{request.TemplateCode}' not found or inactive");
            }

            if (template.Type != request.Type)
            {
                return Result<SendNotificationResultDto>.Failure($"Template type mismatch. Template is for {template.Type}, but request is for {request.Type}");
            }

            templateId = template.Id;
            subject = template.Subject;
            content = template.Body;

            // Replace variables in template
            if (request.Variables != null && request.Variables.Count > 0)
            {
                foreach (var variable in request.Variables)
                {
                    content = content.Replace($"{{{{{variable.Key}}}}}", variable.Value);
                    if (!string.IsNullOrWhiteSpace(subject))
                    {
                        subject = subject.Replace($"{{{{{variable.Key}}}}}", variable.Value);
                    }
                }
            }
        }
        else
        {
            // Use direct content
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Result<SendNotificationResultDto>.Failure("Either template code or content is required");
            }

            content = request.Content;

            // For email, subject is required
            if (request.Type == NotificationType.Email && string.IsNullOrWhiteSpace(subject))
            {
                return Result<SendNotificationResultDto>.Failure("Subject is required for email notifications");
            }
        }

        // Create notification log
        var log = NotificationLog.Create(
            request.Type,
            request.Recipient,
            content,
            subject,
            templateId,
            request.ReferenceType,
            request.ReferenceId);

        _dbContext.NotificationLogs.Add(log);

        // In a real implementation, this would call the actual notification provider
        // For now, we'll simulate sending and mark as sent
        try
        {
            // Simulate sending (in production, this would call email/SMS/WhatsApp providers)
            var providerMessageId = $"MSG-{Guid.NewGuid():N}";

            log.MarkSent(providerMessageId, "Notification queued for delivery");

            _logger.LogInformation(
                "Notification sent: {Type} to {Recipient} (MessageId: {MessageId})",
                request.Type, request.Recipient, providerMessageId);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result<SendNotificationResultDto>.Success(new SendNotificationResultDto
            {
                LogId = log.Id,
                Status = log.Status,
                ProviderMessageId = providerMessageId
            });
        }
        catch (Exception ex)
        {
            log.MarkFailed(ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex,
                "Failed to send notification: {Type} to {Recipient}",
                request.Type, request.Recipient);

            return Result<SendNotificationResultDto>.Success(new SendNotificationResultDto
            {
                LogId = log.Id,
                Status = log.Status,
                ErrorMessage = ex.Message
            });
        }
    }

    private static bool ValidateRecipient(NotificationType type, string recipient, out string error)
    {
        error = string.Empty;

        switch (type)
        {
            case NotificationType.Email:
                if (!IsValidEmail(recipient))
                {
                    error = "Invalid email address";
                    return false;
                }
                break;

            case NotificationType.SMS:
            case NotificationType.WhatsApp:
                if (!IsValidPhoneNumber(recipient))
                {
                    error = "Invalid phone number";
                    return false;
                }
                break;
        }

        return true;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPhoneNumber(string phone)
    {
        // Basic phone validation - accepts formats like +91XXXXXXXXXX or just digits
        var cleanPhone = Regex.Replace(phone, @"[\s\-\(\)]", "");
        return Regex.IsMatch(cleanPhone, @"^\+?\d{10,15}$");
    }
}

/// <summary>
/// Command to send bulk notifications.
/// </summary>
[RequirePermission("notifications.send")]
[RequireFeature("notifications")]
public record SendBulkNotificationCommand : IRequest<Result<List<SendNotificationResultDto>>>, ITenantRequest
{
    public NotificationType Type { get; init; }
    public List<string> Recipients { get; init; } = new();
    public string? TemplateCode { get; init; }
    public string? Subject { get; init; }
    public string? Content { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
}

public class SendBulkNotificationCommandHandler : IRequestHandler<SendBulkNotificationCommand, Result<List<SendNotificationResultDto>>>
{
    private readonly IMediator _mediator;

    public SendBulkNotificationCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<List<SendNotificationResultDto>>> Handle(
        SendBulkNotificationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Recipients.Count == 0)
        {
            return Result<List<SendNotificationResultDto>>.Failure("At least one recipient is required");
        }

        if (request.Recipients.Count > 100)
        {
            return Result<List<SendNotificationResultDto>>.Failure("Maximum 100 recipients allowed per bulk send");
        }

        var results = new List<SendNotificationResultDto>();

        foreach (var recipient in request.Recipients)
        {
            var result = await _mediator.Send(new SendNotificationCommand
            {
                Type = request.Type,
                Recipient = recipient,
                TemplateCode = request.TemplateCode,
                Subject = request.Subject,
                Content = request.Content,
                Variables = request.Variables,
                ReferenceType = request.ReferenceType,
                ReferenceId = request.ReferenceId
            }, cancellationToken);

            if (result.IsSuccess)
            {
                results.Add(result.Value!);
            }
            else
            {
                results.Add(new SendNotificationResultDto
                {
                    LogId = Guid.Empty,
                    Status = "Failed",
                    ErrorMessage = string.Join(", ", result.Errors)
                });
            }
        }

        return Result<List<SendNotificationResultDto>>.Success(results);
    }
}
