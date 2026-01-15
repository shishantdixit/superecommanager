using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Interface for publishing domain events.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a domain event.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : DomainEvent;

    /// <summary>
    /// Publishes multiple domain events.
    /// </summary>
    Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
