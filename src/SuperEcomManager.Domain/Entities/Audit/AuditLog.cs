using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Audit;

/// <summary>
/// Types of audit actions that can be logged.
/// </summary>
public enum AuditAction
{
    // Authentication
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    PasswordChanged = 4,
    PasswordReset = 5,

    // User Management
    UserCreated = 10,
    UserUpdated = 11,
    UserDeleted = 12,
    UserActivated = 13,
    UserDeactivated = 14,
    UserInvited = 15,
    RoleAssigned = 16,
    RoleRemoved = 17,

    // Orders
    OrderCreated = 20,
    OrderUpdated = 21,
    OrderCancelled = 22,
    OrderConfirmed = 23,

    // Shipments
    ShipmentCreated = 30,
    ShipmentUpdated = 31,
    ShipmentCancelled = 32,
    ShipmentManifested = 33,

    // NDR
    NdrCreated = 40,
    NdrAssigned = 41,
    NdrActionTaken = 42,
    NdrResolved = 43,

    // Inventory
    StockAdjusted = 50,
    ProductCreated = 51,
    ProductUpdated = 52,
    ProductDeleted = 53,

    // Settings
    SettingsUpdated = 60,

    // Channels
    ChannelConnected = 70,
    ChannelDisconnected = 71,
    ChannelSynced = 72,

    // Courier
    CourierAccountAdded = 80,
    CourierAccountUpdated = 81,
    CourierAccountRemoved = 82,

    // Finance
    ExpenseCreated = 90,
    ExpenseUpdated = 91,
    ExpenseDeleted = 92,

    // Generic
    DataExported = 100,
    DataImported = 101,
    BulkOperation = 102
}

/// <summary>
/// Module/area where the action occurred.
/// </summary>
public enum AuditModule
{
    Authentication = 1,
    Users = 2,
    Roles = 3,
    Orders = 4,
    Shipments = 5,
    NDR = 6,
    Inventory = 7,
    Settings = 8,
    Channels = 9,
    Couriers = 10,
    Finance = 11,
    System = 12
}

/// <summary>
/// Audit log entry for tracking all significant actions in the system.
/// </summary>
public class AuditLog : BaseEntity
{
    public AuditAction Action { get; private set; }
    public AuditModule Module { get; private set; }

    /// <summary>
    /// User who performed the action (null for system actions).
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Username at time of action (denormalized for history).
    /// </summary>
    public string? UserName { get; private set; }

    /// <summary>
    /// IP address of the request.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent/browser info.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Entity type affected (e.g., "Order", "User").
    /// </summary>
    public string? EntityType { get; private set; }

    /// <summary>
    /// ID of the affected entity.
    /// </summary>
    public Guid? EntityId { get; private set; }

    /// <summary>
    /// Human-readable description of the action.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// JSON of old values (for updates).
    /// </summary>
    public string? OldValues { get; private set; }

    /// <summary>
    /// JSON of new values (for creates/updates).
    /// </summary>
    public string? NewValues { get; private set; }

    /// <summary>
    /// Additional context as JSON.
    /// </summary>
    public string? AdditionalData { get; private set; }

    /// <summary>
    /// Whether the action was successful.
    /// </summary>
    public bool IsSuccess { get; private set; } = true;

    /// <summary>
    /// Error message if action failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        AuditAction action,
        AuditModule module,
        string description,
        Guid? userId = null,
        string? userName = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? entityType = null,
        Guid? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? additionalData = null,
        bool isSuccess = true,
        string? errorMessage = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            Module = module,
            Description = description,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            AdditionalData = additionalData,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create a login audit log.
    /// </summary>
    public static AuditLog CreateLoginLog(
        Guid userId,
        string userName,
        string? ipAddress,
        string? userAgent,
        bool isSuccess,
        string? errorMessage = null)
    {
        return Create(
            isSuccess ? AuditAction.Login : AuditAction.LoginFailed,
            AuditModule.Authentication,
            isSuccess ? $"User {userName} logged in" : $"Failed login attempt for {userName}",
            userId,
            userName,
            ipAddress,
            userAgent,
            "User",
            userId,
            isSuccess: isSuccess,
            errorMessage: errorMessage);
    }

    /// <summary>
    /// Create a logout audit log.
    /// </summary>
    public static AuditLog CreateLogoutLog(
        Guid userId,
        string userName,
        string? ipAddress,
        string? userAgent)
    {
        return Create(
            AuditAction.Logout,
            AuditModule.Authentication,
            $"User {userName} logged out",
            userId,
            userName,
            ipAddress,
            userAgent,
            "User",
            userId);
    }

    /// <summary>
    /// Create an entity change audit log.
    /// </summary>
    public static AuditLog CreateEntityLog(
        AuditAction action,
        AuditModule module,
        string entityType,
        Guid entityId,
        string description,
        Guid? userId,
        string? userName,
        string? ipAddress = null,
        string? oldValues = null,
        string? newValues = null)
    {
        return Create(
            action,
            module,
            description,
            userId,
            userName,
            ipAddress,
            entityType: entityType,
            entityId: entityId,
            oldValues: oldValues,
            newValues: newValues);
    }
}
