using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Services;

/// <summary>
/// Factory for creating order update services based on channel type.
/// </summary>
public class OrderUpdateServiceFactory : IOrderUpdateServiceFactory
{
    private readonly IEnumerable<IOrderUpdateService> _services;

    public OrderUpdateServiceFactory(IEnumerable<IOrderUpdateService> services)
    {
        _services = services;
    }

    public IOrderUpdateService? GetService(ChannelType channelType)
    {
        return _services.FirstOrDefault(s => s.ChannelType == channelType);
    }

    public bool IsSupported(ChannelType channelType)
    {
        return _services.Any(s => s.ChannelType == channelType);
    }
}
