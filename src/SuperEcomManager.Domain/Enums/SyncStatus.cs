namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Represents the synchronization status of an entity with external sales channels.
/// </summary>
public enum SyncStatus
{
    /// <summary>Entity data matches the channel data</summary>
    Synced = 0,

    /// <summary>Entity has local changes that will never sync to channel</summary>
    LocalOnly = 1,

    /// <summary>Entity has changes pending to be pushed to channel</summary>
    Pending = 2,

    /// <summary>Channel data changed after local edit, requires resolution</summary>
    Conflict = 3
}
