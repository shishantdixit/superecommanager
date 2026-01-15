namespace SuperEcomManager.Domain.Common;

/// <summary>
/// Interface for entities that support soft delete.
/// Soft deleted entities are not physically removed from the database.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// The timestamp when this entity was deleted.
    /// Null means the entity is not deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// The user who deleted this entity.
    /// </summary>
    Guid? DeletedBy { get; set; }
}
