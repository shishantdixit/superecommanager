using Microsoft.Extensions.Caching.Distributed;
using SuperEcomManager.Application.Common.Interfaces;
using System.Text.Json;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Implementation of ICacheService using distributed cache (Redis).
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ICurrentTenantService _currentTenantService;

    public CacheService(IDistributedCache cache, ICurrentTenantService currentTenantService)
    {
        _cache = cache;
        _currentTenantService = currentTenantService;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var tenantKey = GetTenantKey(key);
        var data = await _cache.GetStringAsync(tenantKey, cancellationToken);

        if (string.IsNullOrEmpty(data))
            return default;

        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var tenantKey = GetTenantKey(key);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
        };

        var data = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(tenantKey, data, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var tenantKey = GetTenantKey(key);
        await _cache.RemoveAsync(tenantKey, cancellationToken);
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // Note: Standard IDistributedCache doesn't support prefix removal
        // This would need Redis-specific implementation with SCAN command
        // For now, we rely on expiration for cache invalidation
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);

        if (cached != null)
            return cached;

        var value = await factory();
        await SetAsync(key, value, expiration, cancellationToken);

        return value;
    }

    private string GetTenantKey(string key)
    {
        if (_currentTenantService.HasTenant)
            return $"tenant:{_currentTenantService.TenantId}:{key}";

        return $"global:{key}";
    }
}
