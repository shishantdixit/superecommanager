using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Services;

/// <summary>
/// Factory for creating order creation services based on channel type.
/// </summary>
public class OrderCreationServiceFactory : IOrderCreationServiceFactory
{
    private readonly IEnumerable<IOrderCreationService> _services;

    public OrderCreationServiceFactory(IEnumerable<IOrderCreationService> services)
    {
        _services = services;
    }

    public IOrderCreationService? GetService(ChannelType channelType)
    {
        return _services.FirstOrDefault(s => s.ChannelType == channelType);
    }

    public bool IsSupported(ChannelType channelType)
    {
        return _services.Any(s => s.ChannelType == channelType);
    }
}
