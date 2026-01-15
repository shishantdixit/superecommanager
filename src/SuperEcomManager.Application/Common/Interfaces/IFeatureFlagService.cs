namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for checking feature flag status.
/// Features can be enabled/disabled at subscription, tenant, or user level.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if a feature is enabled for the current tenant.
    /// </summary>
    /// <param name="featureCode">The feature code to check.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    Task<bool> IsEnabledAsync(string featureCode);

    /// <summary>
    /// Checks if a feature is enabled for a specific user.
    /// Takes into account user-level feature overrides.
    /// </summary>
    /// <param name="featureCode">The feature code to check.</param>
    /// <param name="userId">The user ID to check for.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    Task<bool> IsEnabledForUserAsync(string featureCode, Guid userId);

    /// <summary>
    /// Gets all enabled features for the current tenant.
    /// </summary>
    /// <returns>List of enabled feature codes.</returns>
    Task<IEnumerable<string>> GetEnabledFeaturesAsync();

    /// <summary>
    /// Gets all enabled features for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of enabled feature codes.</returns>
    Task<IEnumerable<string>> GetEnabledFeaturesForUserAsync(Guid userId);
}
