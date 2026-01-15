namespace SuperEcomManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when a concurrency conflict is detected.
/// </summary>
public class ConcurrencyException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public ConcurrencyException(string entityName, object entityId)
        : base("CONCURRENCY_CONFLICT",
            $"The {entityName} with id '{entityId}' has been modified by another process. Please reload and try again.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public static ConcurrencyException For<TEntity>(object id)
        where TEntity : class
    {
        return new ConcurrencyException(typeof(TEntity).Name, id);
    }
}
