using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that automatically sets audit properties on entities.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public AuditableEntityInterceptor(
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified || HasChangedOwnedEntities(entry))
            {
                var utcNow = _dateTimeService.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                }

                entry.Entity.UpdatedAt = utcNow;
                entry.Entity.UpdatedBy = _currentUserService.UserId;
            }
        }
    }

    private static bool HasChangedOwnedEntities(EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry != null &&
            r.TargetEntry.Metadata.IsOwned() &&
            r.TargetEntry.State is EntityState.Added or EntityState.Modified);
}
