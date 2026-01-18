using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Services;

/// <summary>
/// Factory for creating inventory sync services based on channel type.
/// </summary>
public class InventorySyncServiceFactory : IInventorySyncServiceFactory
{
    private readonly IEnumerable<IInventorySyncService> _services;

    public InventorySyncServiceFactory(IEnumerable<IInventorySyncService> services)
    {
        _services = services;
    }

    public IInventorySyncService? GetService(ChannelType channelType)
    {
        return _services.FirstOrDefault(s => s.ChannelType == channelType);
    }

    public bool IsSupported(ChannelType channelType)
    {
        return _services.Any(s => s.ChannelType == channelType);
    }
}
