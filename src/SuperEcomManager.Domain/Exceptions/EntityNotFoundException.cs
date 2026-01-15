namespace SuperEcomManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when an entity is not found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityName, object entityId)
        : base("ENTITY_NOT_FOUND", $"{entityName} with id '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public static EntityNotFoundException For<TEntity>(object id)
        where TEntity : class
    {
        return new EntityNotFoundException(typeof(TEntity).Name, id);
    }
}
