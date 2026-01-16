using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SuperEcomManager.Infrastructure.RateLimiting;

/// <summary>
/// Service for tracking API usage per tenant.
/// </summary>
public interface IApiUsageTracker
{
    Task RecordRequestAsync(string tenantId, string endpoint, CancellationToken cancellationToken = default);
    Task<ApiUsageStats> GetUsageStatsAsync(string tenantId, int days = 30, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetDailyUsageAsync(string tenantId, DateTime date, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of API usage tracking using distributed cache.
/// </summary>
public class ApiUsageTracker : IApiUsageTracker
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ApiUsageTracker> _logger;
    private const string KeyPrefix = "api_usage:";

    public ApiUsageTracker(IDistributedCache cache, ILogger<ApiUsageTracker> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task RecordRequestAsync(string tenantId, string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var dayKey = GetDayKey(tenantId, today);
            var hourKey = GetHourKey(tenantId, DateTime.UtcNow);

            // Increment daily counter
            await IncrementCounterAsync(dayKey, endpoint, TimeSpan.FromDays(35), cancellationToken);

            // Increment hourly counter (for more granular tracking)
            await IncrementCounterAsync(hourKey, endpoint, TimeSpan.FromHours(25), cancellationToken);

            // Increment total request count for the day
            await IncrementTotalAsync(dayKey, TimeSpan.FromDays(35), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record API usage for tenant {TenantId}", tenantId);
        }
    }

    public async Task<ApiUsageStats> GetUsageStatsAsync(string tenantId, int days = 30, CancellationToken cancellationToken = default)
    {
        var stats = new ApiUsageStats
        {
            TenantId = tenantId,
            FromDate = DateTime.UtcNow.Date.AddDays(-days + 1),
            ToDate = DateTime.UtcNow.Date,
            DailyUsage = new List<DailyUsage>()
        };

        var totalRequests = 0;
        var endpointCounts = new Dictionary<string, int>();

        for (var i = 0; i < days; i++)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var usage = await GetDailyUsageAsync(tenantId, date, cancellationToken);
            var dayTotal = await GetDailyTotalAsync(tenantId, date, cancellationToken);

            stats.DailyUsage.Add(new DailyUsage
            {
                Date = date,
                TotalRequests = dayTotal,
                EndpointCounts = usage
            });

            totalRequests += dayTotal;

            foreach (var kvp in usage)
            {
                if (!endpointCounts.ContainsKey(kvp.Key))
                    endpointCounts[kvp.Key] = 0;
                endpointCounts[kvp.Key] += kvp.Value;
            }
        }

        stats.TotalRequests = totalRequests;
        stats.TopEndpoints = endpointCounts
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToDictionary(x => x.Key, x => x.Value);

        return stats;
    }

    public async Task<Dictionary<string, int>> GetDailyUsageAsync(string tenantId, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            var dayKey = GetDayKey(tenantId, date.Date);
            var json = await _cache.GetStringAsync(dayKey, cancellationToken);

            if (string.IsNullOrEmpty(json))
                return new Dictionary<string, int>();

            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get daily usage for tenant {TenantId}", tenantId);
            return new Dictionary<string, int>();
        }
    }

    private async Task<int> GetDailyTotalAsync(string tenantId, DateTime date, CancellationToken cancellationToken)
    {
        try
        {
            var totalKey = GetDayKey(tenantId, date.Date) + ":total";
            var value = await _cache.GetStringAsync(totalKey, cancellationToken);

            return int.TryParse(value, out var total) ? total : 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task IncrementCounterAsync(string key, string endpoint, TimeSpan expiration, CancellationToken cancellationToken)
    {
        var json = await _cache.GetStringAsync(key, cancellationToken);
        var counts = string.IsNullOrEmpty(json)
            ? new Dictionary<string, int>()
            : JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();

        if (!counts.ContainsKey(endpoint))
            counts[endpoint] = 0;

        counts[endpoint]++;

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(counts),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration },
            cancellationToken);
    }

    private async Task IncrementTotalAsync(string dayKey, TimeSpan expiration, CancellationToken cancellationToken)
    {
        var totalKey = dayKey + ":total";
        var value = await _cache.GetStringAsync(totalKey, cancellationToken);
        var total = int.TryParse(value, out var count) ? count : 0;
        total++;

        await _cache.SetStringAsync(
            totalKey,
            total.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration },
            cancellationToken);
    }

    private static string GetDayKey(string tenantId, DateTime date)
        => $"{KeyPrefix}{tenantId}:day:{date:yyyyMMdd}";

    private static string GetHourKey(string tenantId, DateTime dateTime)
        => $"{KeyPrefix}{tenantId}:hour:{dateTime:yyyyMMddHH}";
}

/// <summary>
/// API usage statistics for a tenant.
/// </summary>
public class ApiUsageStats
{
    public string TenantId { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalRequests { get; set; }
    public Dictionary<string, int> TopEndpoints { get; set; } = new();
    public List<DailyUsage> DailyUsage { get; set; } = new();
}

/// <summary>
/// Daily API usage data.
/// </summary>
public class DailyUsage
{
    public DateTime Date { get; set; }
    public int TotalRequests { get; set; }
    public Dictionary<string, int> EndpointCounts { get; set; } = new();
}
