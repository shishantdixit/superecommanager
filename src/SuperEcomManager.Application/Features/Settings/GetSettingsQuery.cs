using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Settings;

namespace SuperEcomManager.Application.Features.Settings;

/// <summary>
/// Query to get all tenant settings.
/// </summary>
[RequirePermission("settings.view")]
[RequireFeature("settings")]
public record GetSettingsQuery : IRequest<Result<TenantSettingsDto>>, ITenantRequest
{
}

public class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, Result<TenantSettingsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetSettingsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        GetSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        // If no settings exist, create default settings
        if (settings == null)
        {
            settings = TenantSettings.CreateDefault();
            _dbContext.TenantSettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Get related entity names
        string? courierName = null;
        if (settings.DefaultCourierAccountId.HasValue)
        {
            courierName = await _dbContext.CourierAccounts
                .AsNoTracking()
                .Where(c => c.Id == settings.DefaultCourierAccountId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        string? agentName = null;
        if (settings.DefaultNdrAgentId.HasValue)
        {
            agentName = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == settings.DefaultNdrAgentId.Value)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var dto = new TenantSettingsDto
        {
            Id = settings.Id,
            General = new GeneralSettingsDto
            {
                Currency = settings.Currency,
                Timezone = settings.Timezone,
                DateFormat = settings.DateFormat,
                TimeFormat = settings.TimeFormat
            },
            Orders = new OrderSettingsDto
            {
                AutoConfirmOrders = settings.AutoConfirmOrders,
                AutoAssignToDefaultCourier = settings.AutoAssignToDefaultCourier,
                DefaultCourierAccountId = settings.DefaultCourierAccountId,
                DefaultCourierName = courierName,
                OrderProcessingCutoffHour = settings.OrderProcessingCutoffHour,
                EnableCOD = settings.EnableCOD,
                MaxCODAmount = settings.MaxCODAmount
            },
            Shipments = new ShipmentSettingsDto
            {
                AutoCreateShipment = settings.AutoCreateShipment,
                RestockOnRTO = settings.RestockOnRTO,
                DefaultPackageWeight = settings.DefaultPackageWeight,
                DefaultPackageLength = settings.DefaultPackageLength,
                DefaultPackageWidth = settings.DefaultPackageWidth,
                DefaultPackageHeight = settings.DefaultPackageHeight
            },
            Ndr = new NdrSettingsDto
            {
                AutoAssignNdrToAgent = settings.AutoAssignNdrToAgent,
                DefaultNdrAgentId = settings.DefaultNdrAgentId,
                DefaultNdrAgentName = agentName,
                NdrFollowUpIntervalHours = settings.NdrFollowUpIntervalHours,
                MaxNdrAttempts = settings.MaxNdrAttempts,
                EscalateAfterMaxAttempts = settings.EscalateAfterMaxAttempts
            },
            Notifications = new NotificationSettingsDto
            {
                SendOrderConfirmationEmail = settings.SendOrderConfirmationEmail,
                SendOrderConfirmationSms = settings.SendOrderConfirmationSms,
                SendShipmentNotification = settings.SendShipmentNotification,
                SendDeliveryNotification = settings.SendDeliveryNotification,
                SendNdrNotification = settings.SendNdrNotification,
                SendRtoNotification = settings.SendRtoNotification
            },
            Inventory = new InventorySettingsDto
            {
                LowStockThreshold = settings.LowStockThreshold,
                AlertOnLowStock = settings.AlertOnLowStock,
                AlertOnOutOfStock = settings.AlertOnOutOfStock,
                PreventOverselling = settings.PreventOverselling
            },
            Sync = new SyncSettingsDto
            {
                AutoSyncOrders = settings.AutoSyncOrders,
                OrderSyncIntervalMinutes = settings.OrderSyncIntervalMinutes,
                AutoSyncInventory = settings.AutoSyncInventory,
                InventorySyncIntervalMinutes = settings.InventorySyncIntervalMinutes
            },
            Branding = new BrandingSettingsDto
            {
                PrimaryColor = settings.PrimaryColor,
                SecondaryColor = settings.SecondaryColor,
                InvoiceLogoUrl = settings.InvoiceLogoUrl,
                InvoiceFooterText = settings.InvoiceFooterText
            },
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };

        return Result<TenantSettingsDto>.Success(dto);
    }
}
