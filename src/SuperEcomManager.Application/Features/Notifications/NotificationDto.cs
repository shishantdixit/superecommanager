using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Notifications;

#region Template DTOs

/// <summary>
/// DTO for notification template list items.
/// </summary>
public record NotificationTemplateListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public string TypeName => Type.ToString();
    public bool IsActive { get; init; }
    public bool IsSystem { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for notification template details.
/// </summary>
public record NotificationTemplateDetailDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public string TypeName => Type.ToString();
    public string? Subject { get; init; }
    public string Body { get; init; } = string.Empty;
    public List<string> Variables { get; init; } = new();
    public bool IsActive { get; init; }
    public bool IsSystem { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a notification template.
/// </summary>
public record CreateNotificationTemplateDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public string? Subject { get; init; }
    public string Body { get; init; } = string.Empty;
    public List<string>? Variables { get; init; }
}

/// <summary>
/// DTO for updating a notification template.
/// </summary>
public record UpdateNotificationTemplateDto
{
    public string Name { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Body { get; init; } = string.Empty;
}

/// <summary>
/// Filter for template queries.
/// </summary>
public record NotificationTemplateFilterDto
{
    public NotificationType? Type { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsSystem { get; init; }
    public string? SearchTerm { get; init; }
}

#endregion

#region Log DTOs

/// <summary>
/// DTO for notification log list items.
/// </summary>
public record NotificationLogListDto
{
    public Guid Id { get; init; }
    public NotificationType Type { get; init; }
    public string TypeName => Type.ToString();
    public string Recipient { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? FailedAt { get; init; }
}

/// <summary>
/// DTO for notification log details.
/// </summary>
public record NotificationLogDetailDto
{
    public Guid Id { get; init; }
    public NotificationType Type { get; init; }
    public string TypeName => Type.ToString();
    public string Recipient { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ProviderResponse { get; init; }
    public string? ProviderMessageId { get; init; }
    public Guid? TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public string? FailureReason { get; init; }
}

/// <summary>
/// Filter for notification log queries.
/// </summary>
public record NotificationLogFilterDto
{
    public NotificationType? Type { get; init; }
    public string? Status { get; init; }
    public string? Recipient { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

#endregion

#region Send DTOs

/// <summary>
/// DTO for sending a notification.
/// </summary>
public record SendNotificationDto
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

/// <summary>
/// Result of sending a notification.
/// </summary>
public record SendNotificationResultDto
{
    public Guid LogId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ProviderMessageId { get; init; }
    public string? ErrorMessage { get; init; }
}

#endregion

#region Stats DTOs

/// <summary>
/// Notification statistics.
/// </summary>
public record NotificationStatsDto
{
    public int TotalSent { get; init; }
    public int TotalDelivered { get; init; }
    public int TotalFailed { get; init; }
    public int TotalPending { get; init; }
    public decimal DeliveryRate { get; init; }

    public Dictionary<string, int> CountByType { get; init; } = new();
    public Dictionary<string, int> CountByStatus { get; init; } = new();
    public List<DailyNotificationStatsDto> DailyStats { get; init; } = new();
    public List<NotificationTypeStatsDto> TypeStats { get; init; } = new();
}

/// <summary>
/// Daily notification stats.
/// </summary>
public record DailyNotificationStatsDto
{
    public DateTime Date { get; init; }
    public int Sent { get; init; }
    public int Delivered { get; init; }
    public int Failed { get; init; }
}

/// <summary>
/// Stats per notification type.
/// </summary>
public record NotificationTypeStatsDto
{
    public NotificationType Type { get; init; }
    public string TypeName => Type.ToString();
    public int TotalSent { get; init; }
    public int Delivered { get; init; }
    public int Failed { get; init; }
    public decimal DeliveryRate { get; init; }
}

#endregion
