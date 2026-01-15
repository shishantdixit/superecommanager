using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Notifications;

/// <summary>
/// Represents a notification template.
/// </summary>
public class NotificationTemplate : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public string? Subject { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public string? Variables { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSystem { get; private set; }

    private NotificationTemplate() { }

    public static NotificationTemplate Create(
        string code,
        string name,
        NotificationType type,
        string body,
        string? subject = null,
        bool isSystem = false)
    {
        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Type = type,
            Subject = subject,
            Body = body,
            IsActive = true,
            IsSystem = isSystem,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? subject, string body)
    {
        if (IsSystem)
            throw new InvalidOperationException("Cannot modify system templates");
        Name = name;
        Subject = subject;
        Body = body;
        UpdatedAt = DateTime.UtcNow;
    }
}
