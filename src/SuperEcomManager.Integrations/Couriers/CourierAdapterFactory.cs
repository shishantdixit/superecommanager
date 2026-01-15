using Microsoft.Extensions.DependencyInjection;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Couriers;

/// <summary>
/// Factory for resolving courier adapters by type.
/// </summary>
public interface ICourierAdapterFactory
{
    /// <summary>
    /// Gets the adapter for a specific courier type.
    /// </summary>
    ICourierAdapter? GetAdapter(CourierType courierType);

    /// <summary>
    /// Gets all registered adapters.
    /// </summary>
    IEnumerable<ICourierAdapter> GetAllAdapters();

    /// <summary>
    /// Checks if an adapter is available for the specified courier type.
    /// </summary>
    bool HasAdapter(CourierType courierType);
}

/// <summary>
/// Default implementation of courier adapter factory.
/// </summary>
public class CourierAdapterFactory : ICourierAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<CourierType, Type> _adapterTypes = new();

    public CourierAdapterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Registers an adapter type for a courier.
    /// </summary>
    public void RegisterAdapter<TAdapter>(CourierType courierType) where TAdapter : ICourierAdapter
    {
        _adapterTypes[courierType] = typeof(TAdapter);
    }

    public ICourierAdapter? GetAdapter(CourierType courierType)
    {
        if (!_adapterTypes.TryGetValue(courierType, out var adapterType))
            return null;

        return (ICourierAdapter?)_serviceProvider.GetService(adapterType);
    }

    public IEnumerable<ICourierAdapter> GetAllAdapters()
    {
        foreach (var adapterType in _adapterTypes.Values)
        {
            var adapter = (ICourierAdapter?)_serviceProvider.GetService(adapterType);
            if (adapter != null)
                yield return adapter;
        }
    }

    public bool HasAdapter(CourierType courierType)
    {
        return _adapterTypes.ContainsKey(courierType);
    }
}

/// <summary>
/// Builder for registering courier adapters during startup.
/// </summary>
public class CourierAdapterBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<(CourierType Type, Type AdapterType)> _registrations = new();

    public CourierAdapterBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers a courier adapter.
    /// </summary>
    public CourierAdapterBuilder AddAdapter<TAdapter>(CourierType courierType)
        where TAdapter : class, ICourierAdapter
    {
        _services.AddScoped<TAdapter>();
        _registrations.Add((courierType, typeof(TAdapter)));
        return this;
    }

    /// <summary>
    /// Builds the factory and registers it.
    /// </summary>
    public void Build()
    {
        _services.AddSingleton<ICourierAdapterFactory>(sp =>
        {
            var factory = new CourierAdapterFactory(sp);
            foreach (var (type, adapterType) in _registrations)
            {
                var method = typeof(CourierAdapterFactory)
                    .GetMethod(nameof(CourierAdapterFactory.RegisterAdapter))!
                    .MakeGenericMethod(adapterType);
                method.Invoke(factory, new object[] { type });
            }
            return factory;
        });
    }
}

/// <summary>
/// Extension methods for registering courier adapters.
/// </summary>
public static class CourierAdapterExtensions
{
    /// <summary>
    /// Adds courier adapter infrastructure.
    /// </summary>
    public static CourierAdapterBuilder AddCourierAdapters(this IServiceCollection services)
    {
        return new CourierAdapterBuilder(services);
    }
}
