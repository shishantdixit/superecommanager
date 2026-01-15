using SuperEcomManager.Domain.Entities.Audit;

namespace SuperEcomManager.Application.Features.Audit;

/// <summary>
/// DTO for audit log list items.
/// </summary>
public record AuditLogListDto
{
    public Guid Id { get; init; }
    public AuditAction Action { get; init; }
    public string ActionName { get; init; } = string.Empty;
    public AuditModule Module { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// DTO for detailed audit log view.
/// </summary>
public record AuditLogDetailDto
{
    public Guid Id { get; init; }
    public AuditAction Action { get; init; }
    public string ActionName { get; init; } = string.Empty;
    public AuditModule Module { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? AdditionalData { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// DTO for login history entries.
/// </summary>
public record LoginHistoryDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; }
    public AuditAction Action { get; init; }
    public string ActionName { get; init; } = string.Empty;
}

/// <summary>
/// DTO for user activity summary.
/// </summary>
public record UserActivityDto
{
    public Guid Id { get; init; }
    public AuditAction Action { get; init; }
    public string ActionName { get; init; } = string.Empty;
    public AuditModule Module { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// DTO for audit statistics.
/// </summary>
public record AuditStatsDto
{
    public int TotalActions { get; init; }
    public int TotalToday { get; init; }
    public int TotalThisWeek { get; init; }
    public int TotalThisMonth { get; init; }
    public int SuccessfulActions { get; init; }
    public int FailedActions { get; init; }
    public int TotalLogins { get; init; }
    public int FailedLogins { get; init; }
    public List<ModuleActivityDto> ActivityByModule { get; init; } = new();
    public List<ActionCountDto> TopActions { get; init; } = new();
    public List<UserActivitySummaryDto> MostActiveUsers { get; init; } = new();
}

/// <summary>
/// DTO for module activity count.
/// </summary>
public record ModuleActivityDto
{
    public AuditModule Module { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    public int Count { get; init; }
}

/// <summary>
/// DTO for action count.
/// </summary>
public record ActionCountDto
{
    public AuditAction Action { get; init; }
    public string ActionName { get; init; } = string.Empty;
    public int Count { get; init; }
}

/// <summary>
/// DTO for user activity summary.
/// </summary>
public record UserActivitySummaryDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int ActionCount { get; init; }
    public DateTime? LastActivity { get; init; }
}

/// <summary>
/// Helper class for enum name conversion.
/// </summary>
public static class AuditEnumHelper
{
    public static string GetActionName(AuditAction action)
    {
        return action switch
        {
            AuditAction.Login => "Login",
            AuditAction.Logout => "Logout",
            AuditAction.LoginFailed => "Login Failed",
            AuditAction.PasswordChanged => "Password Changed",
            AuditAction.PasswordReset => "Password Reset",
            AuditAction.UserCreated => "User Created",
            AuditAction.UserUpdated => "User Updated",
            AuditAction.UserDeleted => "User Deleted",
            AuditAction.UserActivated => "User Activated",
            AuditAction.UserDeactivated => "User Deactivated",
            AuditAction.UserInvited => "User Invited",
            AuditAction.RoleAssigned => "Role Assigned",
            AuditAction.RoleRemoved => "Role Removed",
            AuditAction.OrderCreated => "Order Created",
            AuditAction.OrderUpdated => "Order Updated",
            AuditAction.OrderCancelled => "Order Cancelled",
            AuditAction.OrderConfirmed => "Order Confirmed",
            AuditAction.ShipmentCreated => "Shipment Created",
            AuditAction.ShipmentUpdated => "Shipment Updated",
            AuditAction.ShipmentCancelled => "Shipment Cancelled",
            AuditAction.ShipmentManifested => "Shipment Manifested",
            AuditAction.NdrCreated => "NDR Created",
            AuditAction.NdrAssigned => "NDR Assigned",
            AuditAction.NdrActionTaken => "NDR Action Taken",
            AuditAction.NdrResolved => "NDR Resolved",
            AuditAction.StockAdjusted => "Stock Adjusted",
            AuditAction.ProductCreated => "Product Created",
            AuditAction.ProductUpdated => "Product Updated",
            AuditAction.ProductDeleted => "Product Deleted",
            AuditAction.SettingsUpdated => "Settings Updated",
            AuditAction.ChannelConnected => "Channel Connected",
            AuditAction.ChannelDisconnected => "Channel Disconnected",
            AuditAction.ChannelSynced => "Channel Synced",
            AuditAction.CourierAccountAdded => "Courier Account Added",
            AuditAction.CourierAccountUpdated => "Courier Account Updated",
            AuditAction.CourierAccountRemoved => "Courier Account Removed",
            AuditAction.ExpenseCreated => "Expense Created",
            AuditAction.ExpenseUpdated => "Expense Updated",
            AuditAction.ExpenseDeleted => "Expense Deleted",
            AuditAction.DataExported => "Data Exported",
            AuditAction.DataImported => "Data Imported",
            AuditAction.BulkOperation => "Bulk Operation",
            _ => action.ToString()
        };
    }

    public static string GetModuleName(AuditModule module)
    {
        return module switch
        {
            AuditModule.Authentication => "Authentication",
            AuditModule.Users => "Users",
            AuditModule.Roles => "Roles",
            AuditModule.Orders => "Orders",
            AuditModule.Shipments => "Shipments",
            AuditModule.NDR => "NDR",
            AuditModule.Inventory => "Inventory",
            AuditModule.Settings => "Settings",
            AuditModule.Channels => "Channels",
            AuditModule.Couriers => "Couriers",
            AuditModule.Finance => "Finance",
            AuditModule.System => "System",
            _ => module.ToString()
        };
    }
}
