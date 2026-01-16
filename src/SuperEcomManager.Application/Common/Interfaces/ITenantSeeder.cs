namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Interface for tenant schema initialization and seeding.
/// </summary>
public interface ITenantSeeder
{
    /// <summary>
    /// Initialize a new tenant's schema with default data.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="schemaName">The schema name to create.</param>
    /// <param name="ownerEmail">The owner's email address.</param>
    /// <param name="ownerPassword">The owner's password.</param>
    /// <param name="companyName">The company name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeTenantAsync(
        Guid tenantId,
        string schemaName,
        string ownerEmail,
        string ownerPassword,
        string companyName,
        CancellationToken cancellationToken = default);
}
