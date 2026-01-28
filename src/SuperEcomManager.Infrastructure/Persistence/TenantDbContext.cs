using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Common;
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
using SuperEcomManager.Domain.Entities.Webhooks;
using SuperEcomManager.Infrastructure.Persistence.Interceptors;

namespace SuperEcomManager.Infrastructure.Persistence;

/// <summary>
/// Database context for tenant-specific data.
/// Uses schema-per-tenant for data isolation.
/// </summary>
public class TenantDbContext : DbContext, ITenantDbContext
{
    private readonly ICurrentTenantService _currentTenantService;
    private readonly AuditableEntityInterceptor _auditableEntityInterceptor;
    private readonly SoftDeleteInterceptor _softDeleteInterceptor;

    /// <summary>
    /// Gets the current tenant's schema name. Used by TenantModelCacheKeyFactory.
    /// </summary>
    public string CurrentSchemaName => _currentTenantService.SchemaName;

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ICurrentTenantService currentTenantService,
        AuditableEntityInterceptor auditableEntityInterceptor,
        SoftDeleteInterceptor softDeleteInterceptor)
        : base(options)
    {
        _currentTenantService = currentTenantService;
        _auditableEntityInterceptor = auditableEntityInterceptor;
        _softDeleteInterceptor = softDeleteInterceptor;
    }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Orders
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();

    // Shipments
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<ShipmentTracking> ShipmentTrackings => Set<ShipmentTracking>();

    // NDR
    public DbSet<NdrRecord> NdrRecords => Set<NdrRecord>();
    public DbSet<NdrAction> NdrActions => Set<NdrAction>();
    public DbSet<NdrRemark> NdrRemarks => Set<NdrRemark>();

    // Inventory
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    // Channels
    public DbSet<SalesChannel> SalesChannels => Set<SalesChannel>();

    // Courier Accounts
    public DbSet<CourierAccount> CourierAccounts => Set<CourierAccount>();

    // Notifications
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    // Finance
    public DbSet<Expense> Expenses => Set<Expense>();

    // Settings
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Webhooks
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        optionsBuilder.AddInterceptors(_softDeleteInterceptor);
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore DomainEvent - it's not a database entity
        modelBuilder.Ignore<DomainEvent>();

        // Set schema based on current tenant
        if (_currentTenantService.HasTenant)
        {
            modelBuilder.HasDefaultSchema(_currentTenantService.SchemaName);
        }

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly,
            type => type.Namespace?.Contains("Configurations.Tenant") ?? false);

        // Apply global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(GenerateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression GenerateSoftDeleteFilter(Type entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
        var nullConstant = System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?));
        var comparison = System.Linq.Expressions.Expression.Equal(property, nullConstant);
        return System.Linq.Expressions.Expression.Lambda(comparison, parameter);
    }
}
