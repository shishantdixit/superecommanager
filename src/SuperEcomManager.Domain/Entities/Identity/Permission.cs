using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Identity;

/// <summary>
/// Represents a permission that can be assigned to roles.
/// Stored in shared schema - same for all tenants.
/// </summary>
public class Permission : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Permission() { } // EF Core constructor

    public static Permission Create(string code, string name, string module, string? description = null)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Module = module,
            Description = description
        };
    }

    /// <summary>
    /// Creates all system permissions.
    /// </summary>
    public static IEnumerable<Permission> CreateAllPermissions()
    {
        // Orders module
        yield return Create("orders.view", "View Orders", "orders", "View order list and details");
        yield return Create("orders.create", "Create Orders", "orders", "Create new orders");
        yield return Create("orders.edit", "Edit Orders", "orders", "Edit order details");
        yield return Create("orders.cancel", "Cancel Orders", "orders", "Cancel orders");
        yield return Create("orders.export", "Export Orders", "orders", "Export order data");
        yield return Create("orders.bulk", "Bulk Order Operations", "orders", "Perform bulk updates on orders");

        // Shipments module
        yield return Create("shipments.view", "View Shipments", "shipments", "View shipment list and details");
        yield return Create("shipments.create", "Create Shipments", "shipments", "Create new shipments");
        yield return Create("shipments.cancel", "Cancel Shipments", "shipments", "Cancel shipments");
        yield return Create("shipments.track", "Track Shipments", "shipments", "View tracking information");
        yield return Create("shipments.bulk", "Bulk Shipment Operations", "shipments", "Perform bulk shipment creation");
        yield return Create("shipments.export", "Export Shipments", "shipments", "Export shipment data");

        // NDR module
        yield return Create("ndr.view", "View NDR", "ndr", "View NDR inbox and records");
        yield return Create("ndr.action", "NDR Actions", "ndr", "Perform NDR actions (call, message)");
        yield return Create("ndr.assign", "Assign NDR", "ndr", "Assign NDR to employees");
        yield return Create("ndr.reattempt", "Schedule Reattempt", "ndr", "Schedule delivery reattempts");
        yield return Create("ndr.export", "Export NDR", "ndr", "Export NDR data");
        yield return Create("ndr.bulk", "Bulk NDR Operations", "ndr", "Perform bulk NDR assign/status updates");

        // Inventory module
        yield return Create("inventory.view", "View Inventory", "inventory", "View products and stock levels");
        yield return Create("inventory.create", "Create Products", "inventory", "Create new products");
        yield return Create("inventory.edit", "Edit Products", "inventory", "Edit product details");
        yield return Create("inventory.adjust", "Adjust Stock", "inventory", "Adjust stock levels");
        yield return Create("inventory.export", "Export Inventory", "inventory", "Export inventory data");

        // Channels module
        yield return Create("channels.view", "View Channels", "channels", "View connected sales channels");
        yield return Create("channels.connect", "Connect Channels", "channels", "Connect new sales channels");
        yield return Create("channels.disconnect", "Disconnect Channels", "channels", "Disconnect channels");
        yield return Create("channels.settings", "Channel Settings", "channels", "Manage channel settings");
        yield return Create("channels.sync", "Sync Channels", "channels", "Trigger manual order and inventory sync");

        // Team module
        yield return Create("team.view", "View Team", "team", "View team members");
        yield return Create("team.invite", "Invite Users", "team", "Invite new team members");
        yield return Create("team.edit", "Edit Users", "team", "Edit user details");
        yield return Create("team.delete", "Delete Users", "team", "Remove team members");
        yield return Create("team.roles", "Manage Roles", "team", "Create and manage roles");

        // Finance module
        yield return Create("finance.view", "View Finance", "finance", "View financial reports");
        yield return Create("finance.create", "Create Expenses", "finance", "Record expenses");
        yield return Create("finance.export", "Export Finance", "finance", "Export financial data");

        // Settings module
        yield return Create("settings.view", "View Settings", "settings", "View tenant settings");
        yield return Create("settings.edit", "Edit Settings", "settings", "Modify tenant settings");

        // Analytics module
        yield return Create("analytics.view", "View Analytics", "analytics", "View analytics dashboard");
        yield return Create("analytics.export", "Export Analytics", "analytics", "Export analytics reports");

        // Webhooks module
        yield return Create("webhooks.view", "View Webhooks", "webhooks", "View webhook subscriptions and logs");
        yield return Create("webhooks.manage", "Manage Webhooks", "webhooks", "Create, edit, and delete webhook subscriptions");

        // Audit module
        yield return Create("audit.view", "View Audit Logs", "audit", "View audit log entries");
        yield return Create("audit.export", "Export Audit Logs", "audit", "Export audit log data");

        // Security module
        yield return Create("security.view", "View Security", "security", "View security settings");
        yield return Create("security.configure", "Configure Security", "security", "Modify security settings");
        yield return Create("security.audit_logs", "View Audit Logs", "security", "Access audit logs");
        yield return Create("security.export_approve", "Approve Exports", "security", "Approve large exports");
        yield return Create("security.sessions", "Manage Sessions", "security", "View and manage user sessions");
        yield return Create("security.force_logout", "Force Logout", "security", "Force logout users");

        // Data access module
        yield return Create("data.view_masked", "View Masked Data", "data_access", "View masked sensitive data");
        yield return Create("data.view_full", "View Full Data", "data_access", "View unmasked data");
        yield return Create("data.copy", "Copy Data", "data_access", "Copy data from UI");
        yield return Create("data.print", "Print Data", "data_access", "Print pages");

        // Export permissions
        yield return Create("export.orders_csv", "Export Orders CSV", "export", "Export orders as CSV");
        yield return Create("export.orders_excel", "Export Orders Excel", "export", "Export orders as Excel");
        yield return Create("export.customers", "Export Customers", "export", "Export customer data");
        yield return Create("export.financial", "Export Financial", "export", "Export financial data");
        yield return Create("export.ndr", "Export NDR", "export", "Export NDR records");
        yield return Create("export.inventory", "Export Inventory", "export", "Export inventory data");
        yield return Create("export.analytics", "Export Analytics", "export", "Export analytics reports");
        yield return Create("export.bulk_api", "Bulk API Export", "export", "Access bulk export API");
    }
}
