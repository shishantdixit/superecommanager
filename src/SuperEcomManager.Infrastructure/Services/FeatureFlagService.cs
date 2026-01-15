using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Infrastructure.Persistence;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Implementation of IFeatureFlagService.
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly ApplicationDbContext _applicationContext;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICacheService _cacheService;

    public FeatureFlagService(
        ApplicationDbContext applicationContext,
        ICurrentTenantService currentTenantService,
        ICacheService cacheService)
    {
        _applicationContext = applicationContext;
        _currentTenantService = currentTenantService;
        _cacheService = cacheService;
    }

    public async Task<bool> IsEnabledAsync(string featureCode)
    {
        if (!_currentTenantService.HasTenant)
            return false;

        var enabledFeatures = await GetEnabledFeaturesAsync();
        return enabledFeatures.Contains(featureCode);
    }

    public async Task<bool> IsEnabledForUserAsync(string featureCode, Guid userId)
    {
        // First check tenant-level features
        if (!await IsEnabledAsync(featureCode))
            return false;

        // User-level overrides could be implemented here
        // For now, just return tenant-level result
        return true;
    }

    public async Task<IEnumerable<string>> GetEnabledFeaturesAsync()
    {
        if (!_currentTenantService.HasTenant)
            return Enumerable.Empty<string>();

        var cacheKey = $"features:tenant:{_currentTenantService.TenantId}";

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                // Get the active subscription for the tenant
                var subscription = await _applicationContext.Subscriptions
                    .Include(s => s.Plan)
                        .ThenInclude(p => p.PlanFeatures)
                            .ThenInclude(pf => pf.Feature)
                    .Where(s => s.TenantId == _currentTenantService.TenantId)
                    .Where(s => s.Status == Domain.Enums.SubscriptionStatus.Active)
                    .FirstOrDefaultAsync();

                if (subscription == null)
                    return Enumerable.Empty<string>();

                var features = subscription.Plan.PlanFeatures
                    .Select(pf => pf.Feature.Code)
                    .ToList();

                return features.AsEnumerable();
            },
            TimeSpan.FromMinutes(30));
    }

    public async Task<IEnumerable<string>> GetEnabledFeaturesForUserAsync(Guid userId)
    {
        // For now, same as tenant-level features
        // User-level overrides could be implemented here
        return await GetEnabledFeaturesAsync();
    }
}
