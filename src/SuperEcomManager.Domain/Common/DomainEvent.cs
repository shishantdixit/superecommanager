using MediatR;

namespace SuperEcomManager.Domain.Common;

/// <summary>
/// Base class for domain events.
/// Domain events are used for cross-aggregate communication and eventual consistency.
/// </summary>
public abstract class DomainEvent : INotification
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
