using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that converts Delete operations to soft delete.
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public SoftDeleteInterceptor(
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
        SoftDeleteEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SoftDeleteEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SoftDeleteEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAt = _dateTimeService.UtcNow;
                entry.Entity.DeletedBy = _currentUserService.UserId;
            }
        }
    }
}
