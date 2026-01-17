using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Common;

/// <summary>
/// Factory for getting the appropriate channel sync service.
/// </summary>
public class ChannelSyncServiceFactory : IChannelSyncServiceFactory
{
    private readonly IEnumerable<IChannelSyncService> _services;

    public ChannelSyncServiceFactory(IEnumerable<IChannelSyncService> services)
    {
        _services = services;
    }

    public IChannelSyncService? GetService(ChannelType channelType)
    {
        return _services.FirstOrDefault(s => s.ChannelType == channelType);
    }
}
