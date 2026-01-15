namespace SuperEcomManager.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when tenant is not found or not resolved.
/// </summary>
public class TenantNotFoundException : Exception
{
    public TenantNotFoundException()
        : base("Tenant could not be resolved.")
    {
    }

    public TenantNotFoundException(string identifier)
        : base($"Tenant with identifier '{identifier}' was not found.")
    {
    }

    public TenantNotFoundException(Guid tenantId)
        : base($"Tenant with ID '{tenantId}' was not found.")
    {
    }
}
