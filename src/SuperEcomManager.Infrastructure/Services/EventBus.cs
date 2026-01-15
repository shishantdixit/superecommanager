using MediatR;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Implementation of IEventBus using MediatR for in-process event publishing.
/// </summary>
public class EventBus : IEventBus
{
    private readonly IPublisher _publisher;
    private readonly ILogger<EventBus> _logger;

    public EventBus(IPublisher publisher, ILogger<EventBus> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : DomainEvent
    {
        var eventName = domainEvent.GetType().Name;

        _logger.LogInformation("Publishing domain event: {EventName} with ID: {EventId}",
            eventName, domainEvent.EventId);

        try
        {
            await _publisher.Publish(domainEvent, cancellationToken);

            _logger.LogInformation("Successfully published domain event: {EventName}", eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing domain event: {EventName}", eventName);
            throw;
        }
    }

    public async Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await PublishAsync(domainEvent, cancellationToken);
        }
    }
}
