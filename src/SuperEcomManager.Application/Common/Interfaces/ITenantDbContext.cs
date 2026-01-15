using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Domain.Entities.Audit;
using SuperEcomManager.Domain.Entities.Channels;
using SuperEcomManager.Domain.Entities.Finance;
using SuperEcomManager.Domain.Entities.Identity;
using SuperEcomManager.Domain.Entities.Inventory;
using SuperEcomManager.Domain.Entities.NDR;
using SuperEcomManager.Domain.Entities.Notifications;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Entities.Settings;
using SuperEcomManager.Domain.Entities.Shipments;
using SuperEcomManager.Domain.Entities.Shipping;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Interface for the tenant-specific database context.
/// Contains entities that are isolated per tenant.
/// </summary>
public interface ITenantDbContext
{
    // Identity
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    // Orders
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderStatusHistory> OrderStatusHistory { get; }

    // Shipments
    DbSet<Shipment> Shipments { get; }
    DbSet<ShipmentItem> ShipmentItems { get; }
    DbSet<ShipmentTracking> ShipmentTrackings { get; }

    // NDR
    DbSet<NdrRecord> NdrRecords { get; }
    DbSet<NdrAction> NdrActions { get; }
    DbSet<NdrRemark> NdrRemarks { get; }

    // Inventory
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<InventoryItem> Inventory { get; }
    DbSet<StockMovement> StockMovements { get; }

    // Channels
    DbSet<SalesChannel> SalesChannels { get; }

    // Courier Accounts
    DbSet<CourierAccount> CourierAccounts { get; }

    // Notifications
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<NotificationLog> NotificationLogs { get; }

    // Finance
    DbSet<Expense> Expenses { get; }

    // Settings
    DbSet<TenantSettings> TenantSettings { get; }

    // Audit
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
