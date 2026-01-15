using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Subscriptions;

/// <summary>
/// Represents a feature that can be enabled/disabled.
/// Stored in shared schema.
/// </summary>
public class Feature : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsCore { get; private set; }

    private Feature() { }

    public static Feature Create(string code, string name, string module, string? description = null, bool isCore = false)
    {
        return new Feature
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Module = module,
            Description = description,
            IsCore = isCore
        };
    }

    public static IEnumerable<Feature> CreateAllFeatures()
    {
        yield return Create("orders_management", "Orders Management", "orders", "Manage orders from all channels", true);
        yield return Create("shipments_management", "Shipments Management", "shipments", "Create and manage shipments", true);
        yield return Create("ndr_management", "NDR Management", "ndr", "Handle non-delivery reports");
        yield return Create("inventory_management", "Inventory Management", "inventory", "Manage products and stock");
        yield return Create("multi_channel", "Multi-Channel Integration", "channels", "Connect multiple sales channels");
        yield return Create("analytics_basic", "Basic Analytics", "analytics", "View basic reports", true);
        yield return Create("analytics_advanced", "Advanced Analytics", "analytics", "Advanced reports and insights");
        yield return Create("team_management", "Team Management", "team", "Manage team members and roles");
        yield return Create("finance_management", "Finance Management", "finance", "Track expenses and P&L");
        yield return Create("bulk_operations", "Bulk Operations", "operations", "Bulk order/shipment operations");
        yield return Create("api_access", "API Access", "api", "Access REST APIs");
        yield return Create("webhooks", "Webhooks", "api", "Configure webhooks");
        yield return Create("custom_branding", "Custom Branding", "settings", "White-label branding");
        yield return Create("priority_support", "Priority Support", "support", "Priority customer support");
    }
}
