using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Dashboard;

/// <summary>
/// Query to get dashboard alerts and action items.
/// </summary>
[RequirePermission("dashboard.view")]
[RequireFeature("dashboard")]
public record GetDashboardAlertsQuery : IRequest<Result<DashboardAlertsDto>>, ITenantRequest
{
}

public class GetDashboardAlertsQueryHandler : IRequestHandler<GetDashboardAlertsQuery, Result<DashboardAlertsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetDashboardAlertsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DashboardAlertsDto>> Handle(
        GetDashboardAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var criticalAlerts = new List<AlertItemDto>();
        var warningAlerts = new List<AlertItemDto>();
        var infoAlerts = new List<AlertItemDto>();

        // Critical: Out of stock products
        var outOfStockCount = await _dbContext.Inventory
            .AsNoTracking()
            .Where(i => i.QuantityOnHand == 0)
            .Select(i => i.ProductId)
            .Distinct()
            .CountAsync(cancellationToken);

        if (outOfStockCount > 0)
        {
            criticalAlerts.Add(new AlertItemDto
            {
                Type = "inventory",
                Severity = "critical",
                Title = "Out of Stock Products",
                Message = $"{outOfStockCount} product(s) are completely out of stock and cannot be fulfilled.",
                Count = outOfStockCount,
                ActionUrl = "/inventory?filter=outOfStock",
                CreatedAt = now
            });
        }

        // Critical: Escalated NDR cases
        var escalatedNdrCount = await _dbContext.NdrRecords
            .AsNoTracking()
            .CountAsync(n => n.Status == NdrStatus.Escalated, cancellationToken);

        if (escalatedNdrCount > 0)
        {
            criticalAlerts.Add(new AlertItemDto
            {
                Type = "ndr",
                Severity = "critical",
                Title = "Escalated NDR Cases",
                Message = $"{escalatedNdrCount} NDR case(s) have been escalated and require immediate attention.",
                Count = escalatedNdrCount,
                ActionUrl = "/ndr?status=Escalated",
                CreatedAt = now
            });
        }

        // Critical: Failed notifications (last 24 hours)
        var failedNotificationsCount = await _dbContext.NotificationLogs
            .AsNoTracking()
            .CountAsync(n => n.Status == "Failed" && n.FailedAt >= now.AddHours(-24), cancellationToken);

        if (failedNotificationsCount > 5)
        {
            criticalAlerts.Add(new AlertItemDto
            {
                Type = "notifications",
                Severity = "critical",
                Title = "Notification Delivery Failures",
                Message = $"{failedNotificationsCount} notification(s) failed to send in the last 24 hours.",
                Count = failedNotificationsCount,
                ActionUrl = "/notifications/logs?status=Failed",
                CreatedAt = now
            });
        }

        // Warning: Low stock products
        var inventoryItems = await _dbContext.Inventory
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var lowStockCount = inventoryItems
            .Where(i => i.IsLowStock() && i.QuantityOnHand > 0)
            .Select(i => i.ProductId)
            .Distinct()
            .Count();

        if (lowStockCount > 0)
        {
            warningAlerts.Add(new AlertItemDto
            {
                Type = "inventory",
                Severity = "warning",
                Title = "Low Stock Products",
                Message = $"{lowStockCount} product(s) are running low on stock and may need reordering.",
                Count = lowStockCount,
                ActionUrl = "/inventory?filter=lowStock",
                CreatedAt = now
            });
        }

        // Warning: Open NDR cases older than 3 days
        var oldNdrCount = await _dbContext.NdrRecords
            .AsNoTracking()
            .CountAsync(n =>
                (n.Status == NdrStatus.Open || n.Status == NdrStatus.Assigned || n.Status == NdrStatus.CustomerContacted) &&
                n.CreatedAt < now.AddDays(-3), cancellationToken);

        if (oldNdrCount > 0)
        {
            warningAlerts.Add(new AlertItemDto
            {
                Type = "ndr",
                Severity = "warning",
                Title = "Aging NDR Cases",
                Message = $"{oldNdrCount} NDR case(s) have been open for more than 3 days.",
                Count = oldNdrCount,
                ActionUrl = "/ndr?status=Open",
                CreatedAt = now
            });
        }

        // Warning: Pending orders older than 24 hours
        var oldPendingOrdersCount = await _dbContext.Orders
            .AsNoTracking()
            .CountAsync(o =>
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed) &&
                o.CreatedAt < now.AddHours(-24), cancellationToken);

        if (oldPendingOrdersCount > 0)
        {
            warningAlerts.Add(new AlertItemDto
            {
                Type = "orders",
                Severity = "warning",
                Title = "Pending Orders",
                Message = $"{oldPendingOrdersCount} order(s) have been pending for more than 24 hours.",
                Count = oldPendingOrdersCount,
                ActionUrl = "/orders?status=Pending",
                CreatedAt = now
            });
        }

        // Warning: Shipments stuck in transit for more than 5 days
        var stuckShipmentsCount = await _dbContext.Shipments
            .AsNoTracking()
            .CountAsync(s =>
                s.Status == ShipmentStatus.InTransit &&
                s.PickedUpAt.HasValue &&
                s.PickedUpAt.Value < now.AddDays(-5), cancellationToken);

        if (stuckShipmentsCount > 0)
        {
            warningAlerts.Add(new AlertItemDto
            {
                Type = "shipments",
                Severity = "warning",
                Title = "Delayed Shipments",
                Message = $"{stuckShipmentsCount} shipment(s) have been in transit for more than 5 days.",
                Count = stuckShipmentsCount,
                ActionUrl = "/shipments?status=InTransit",
                CreatedAt = now
            });
        }

        // Info: Orders ready to ship (manifested but not picked up)
        var readyToShipCount = await _dbContext.Shipments
            .AsNoTracking()
            .CountAsync(s => s.Status == ShipmentStatus.Manifested, cancellationToken);

        if (readyToShipCount > 0)
        {
            infoAlerts.Add(new AlertItemDto
            {
                Type = "shipments",
                Severity = "info",
                Title = "Ready to Ship",
                Message = $"{readyToShipCount} shipment(s) are ready to be dispatched.",
                Count = readyToShipCount,
                ActionUrl = "/shipments?status=Manifested",
                CreatedAt = now
            });
        }

        // Info: New orders today
        var todayStart = now.Date;
        var newOrdersTodayCount = await _dbContext.Orders
            .AsNoTracking()
            .CountAsync(o => o.CreatedAt >= todayStart, cancellationToken);

        if (newOrdersTodayCount > 0)
        {
            infoAlerts.Add(new AlertItemDto
            {
                Type = "orders",
                Severity = "info",
                Title = "New Orders Today",
                Message = $"{newOrdersTodayCount} new order(s) received today.",
                Count = newOrdersTodayCount,
                ActionUrl = "/orders?date=today",
                CreatedAt = now
            });
        }

        // Info: Inactive channels
        var inactiveChannelsCount = await _dbContext.SalesChannels
            .AsNoTracking()
            .CountAsync(c => !c.IsActive, cancellationToken);

        if (inactiveChannelsCount > 0)
        {
            infoAlerts.Add(new AlertItemDto
            {
                Type = "channels",
                Severity = "info",
                Title = "Inactive Sales Channels",
                Message = $"{inactiveChannelsCount} sales channel(s) are currently inactive.",
                Count = inactiveChannelsCount,
                ActionUrl = "/channels?status=inactive",
                CreatedAt = now
            });
        }

        var totalAlerts = criticalAlerts.Count + warningAlerts.Count + infoAlerts.Count;

        var alerts = new DashboardAlertsDto
        {
            TotalAlerts = totalAlerts,
            CriticalAlerts = criticalAlerts,
            WarningAlerts = warningAlerts,
            InfoAlerts = infoAlerts
        };

        return Result<DashboardAlertsDto>.Success(alerts);
    }
}
