using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SuperEcomManager.Application.Common.Interfaces;

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
            var currentTenantService = tenantContext.GetService<ICurrentTenantService>();
            var schemaName = currentTenantService?.SchemaName ?? "public";
            return (context.GetType(), schemaName, designTime);
        }

        return (context.GetType(), designTime);
    }
}
