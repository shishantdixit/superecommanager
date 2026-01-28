using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SuperEcomManager.Infrastructure.Persistence;

/// <summary>
/// Custom model cache key factory that varies the cache by tenant schema.
/// This ensures each tenant uses the correct schema in the model.
/// </summary>
public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is TenantDbContext tenantContext)
        {
            // Access schema directly from the context to ensure consistency
            // with the schema used in OnModelCreating
            var schemaName = tenantContext.CurrentSchemaName;
            return (context.GetType(), schemaName, designTime);
        }

        return (context.GetType(), designTime);
    }
}
