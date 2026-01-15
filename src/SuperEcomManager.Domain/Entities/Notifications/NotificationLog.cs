using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Notifications;

/// <summary>
/// Log of sent notifications.
/// </summary>
public class NotificationLog : BaseEntity
{
    public NotificationType Type { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? ProviderResponse { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public Guid? TemplateId { get; private set; }
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public string? FailureReason { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(
        NotificationType type,
        string recipient,
        string content,
        string? subject = null,
        Guid? templateId = null,
        string? referenceType = null,
        string? referenceId = null)
    {
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            Type = type,
            Recipient = recipient,
            Subject = subject,
            Content = content,
            Status = "Pending",
            TemplateId = templateId,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkSent(string? providerMessageId, string? providerResponse = null)
    {
        Status = "Sent";
        SentAt = DateTime.UtcNow;
        ProviderMessageId = providerMessageId;
        ProviderResponse = providerResponse;
    }

    public void MarkDelivered()
    {
        Status = "Delivered";
        DeliveredAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason, string? providerResponse = null)
    {
        Status = "Failed";
        FailedAt = DateTime.UtcNow;
        FailureReason = reason;
        ProviderResponse = providerResponse;
    }
}
