using Microsoft.Extensions.DependencyInjection;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Common;

/// <summary>
/// Factory for creating channel adapters based on channel type.
/// </summary>
public interface IChannelAdapterFactory
{
    /// <summary>
    /// Gets an adapter for the specified channel type.
    /// </summary>
    IChannelAdapter GetAdapter(ChannelType channelType);

    /// <summary>
    /// Tries to get an adapter for the specified channel type.
    /// </summary>
    bool TryGetAdapter(ChannelType channelType, out IChannelAdapter? adapter);

    /// <summary>
    /// Gets all registered adapters.
    /// </summary>
    IEnumerable<IChannelAdapter> GetAllAdapters();

    /// <summary>
    /// Checks if an adapter is available for the channel type.
    /// </summary>
    bool IsSupported(ChannelType channelType);
}

/// <summary>
/// Implementation of channel adapter factory.
/// </summary>
public class ChannelAdapterFactory : IChannelAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;
    internal readonly Dictionary<ChannelType, Type> _adapterTypes = new();

    public ChannelAdapterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Registers an adapter type for a channel type.
    /// </summary>
    public void RegisterAdapter<TAdapter>(ChannelType channelType) where TAdapter : IChannelAdapter
    {
        _adapterTypes[channelType] = typeof(TAdapter);
    }

    public IChannelAdapter GetAdapter(ChannelType channelType)
    {
        if (!TryGetAdapter(channelType, out var adapter) || adapter == null)
        {
            throw new NotSupportedException($"No adapter registered for channel type: {channelType}");
        }

        return adapter;
    }

    public bool TryGetAdapter(ChannelType channelType, out IChannelAdapter? adapter)
    {
        adapter = null;

        if (!_adapterTypes.TryGetValue(channelType, out var adapterType))
        {
            return false;
        }

        adapter = (IChannelAdapter)_serviceProvider.GetRequiredService(adapterType);
        return true;
    }

    public IEnumerable<IChannelAdapter> GetAllAdapters()
    {
        foreach (var adapterType in _adapterTypes.Values)
        {
            yield return (IChannelAdapter)_serviceProvider.GetRequiredService(adapterType);
        }
    }

    public bool IsSupported(ChannelType channelType)
    {
        return _adapterTypes.ContainsKey(channelType);
    }
}

/// <summary>
/// Builder for configuring channel adapters.
/// </summary>
public class ChannelAdapterBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<(ChannelType Type, Type AdapterType)> _adapters = new();

    public ChannelAdapterBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Adds a channel adapter to the factory.
    /// </summary>
    public ChannelAdapterBuilder AddAdapter<TAdapter>(ChannelType channelType)
        where TAdapter : class, IChannelAdapter
    {
        _services.AddScoped<TAdapter>();
        _adapters.Add((channelType, typeof(TAdapter)));
        return this;
    }

    /// <summary>
    /// Builds the channel adapter factory with all registered adapters.
    /// </summary>
    internal IServiceCollection Build()
    {
        _services.AddSingleton<IChannelAdapterFactory>(sp =>
        {
            var factory = new ChannelAdapterFactory(sp);
            foreach (var (channelType, adapterType) in _adapters)
            {
                factory._adapterTypes[channelType] = adapterType;
            }
            return factory;
        });

        return _services;
    }
}

/// <summary>
/// Extension methods for registering channel adapters.
/// </summary>
public static class ChannelAdapterExtensions
{
    /// <summary>
    /// Adds channel adapter services to the service collection.
    /// </summary>
    public static ChannelAdapterBuilder AddChannelAdapters(this IServiceCollection services)
    {
        return new ChannelAdapterBuilder(services);
    }

    /// <summary>
    /// Completes the channel adapter registration.
    /// </summary>
    public static IServiceCollection BuildChannelAdapters(this ChannelAdapterBuilder builder)
    {
        return builder.Build();
    }
}
