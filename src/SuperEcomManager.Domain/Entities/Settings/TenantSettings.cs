using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Settings;

/// <summary>
/// Represents tenant-specific configuration settings.
/// Each tenant has one settings record.
/// </summary>
public class TenantSettings : AuditableEntity
{
    // General Settings
    public string Currency { get; private set; } = "INR";
    public string Timezone { get; private set; } = "Asia/Kolkata";
    public string DateFormat { get; private set; } = "dd/MM/yyyy";
    public string TimeFormat { get; private set; } = "HH:mm";

    // Order Settings
    public bool AutoConfirmOrders { get; private set; } = false;
    public bool AutoAssignToDefaultCourier { get; private set; } = false;
    public Guid? DefaultCourierAccountId { get; private set; }
    public int OrderProcessingCutoffHour { get; private set; } = 18; // 6 PM
    public bool EnableCOD { get; private set; } = true;
    public decimal? MaxCODAmount { get; private set; }

    // Shipment Settings
    public bool AutoCreateShipment { get; private set; } = false;
    public bool RestockOnRTO { get; private set; } = true;
    public int DefaultPackageWeight { get; private set; } = 500; // grams
    public int DefaultPackageLength { get; private set; } = 20; // cm
    public int DefaultPackageWidth { get; private set; } = 15; // cm
    public int DefaultPackageHeight { get; private set; } = 10; // cm

    // NDR Settings
    public bool AutoAssignNdrToAgent { get; private set; } = false;
    public Guid? DefaultNdrAgentId { get; private set; }
    public int NdrFollowUpIntervalHours { get; private set; } = 24;
    public int MaxNdrAttempts { get; private set; } = 3;
    public bool EscalateAfterMaxAttempts { get; private set; } = true;

    // Notification Settings
    public bool SendOrderConfirmationEmail { get; private set; } = true;
    public bool SendOrderConfirmationSms { get; private set; } = false;
    public bool SendShipmentNotification { get; private set; } = true;
    public bool SendDeliveryNotification { get; private set; } = true;
    public bool SendNdrNotification { get; private set; } = true;
    public bool SendRtoNotification { get; private set; } = true;

    // Inventory Settings
    public int LowStockThreshold { get; private set; } = 10;
    public bool AlertOnLowStock { get; private set; } = true;
    public bool AlertOnOutOfStock { get; private set; } = true;
    public bool PreventOverselling { get; private set; } = true;

    // Sync Settings
    public bool AutoSyncOrders { get; private set; } = true;
    public int OrderSyncIntervalMinutes { get; private set; } = 15;
    public bool AutoSyncInventory { get; private set; } = false;
    public int InventorySyncIntervalMinutes { get; private set; } = 60;

    // Branding (stored here for quick access, Tenant has profile info)
    public string? PrimaryColor { get; private set; }
    public string? SecondaryColor { get; private set; }
    public string? InvoiceLogoUrl { get; private set; }
    public string? InvoiceFooterText { get; private set; }

    private TenantSettings() { }

    public static TenantSettings CreateDefault()
    {
        return new TenantSettings
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
    }

    // General Settings
    public void UpdateGeneralSettings(
        string currency,
        string timezone,
        string dateFormat,
        string timeFormat)
    {
        Currency = currency ?? Currency;
        Timezone = timezone ?? Timezone;
        DateFormat = dateFormat ?? DateFormat;
        TimeFormat = timeFormat ?? TimeFormat;
        UpdatedAt = DateTime.UtcNow;
    }

    // Order Settings
    public void UpdateOrderSettings(
        bool autoConfirmOrders,
        bool autoAssignToDefaultCourier,
        Guid? defaultCourierAccountId,
        int orderProcessingCutoffHour,
        bool enableCOD,
        decimal? maxCODAmount)
    {
        AutoConfirmOrders = autoConfirmOrders;
        AutoAssignToDefaultCourier = autoAssignToDefaultCourier;
        DefaultCourierAccountId = defaultCourierAccountId;
        OrderProcessingCutoffHour = Math.Clamp(orderProcessingCutoffHour, 0, 23);
        EnableCOD = enableCOD;
        MaxCODAmount = maxCODAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    // Shipment Settings
    public void UpdateShipmentSettings(
        bool autoCreateShipment,
        bool restockOnRTO,
        int defaultPackageWeight,
        int defaultPackageLength,
        int defaultPackageWidth,
        int defaultPackageHeight)
    {
        AutoCreateShipment = autoCreateShipment;
        RestockOnRTO = restockOnRTO;
        DefaultPackageWeight = Math.Max(1, defaultPackageWeight);
        DefaultPackageLength = Math.Max(1, defaultPackageLength);
        DefaultPackageWidth = Math.Max(1, defaultPackageWidth);
        DefaultPackageHeight = Math.Max(1, defaultPackageHeight);
        UpdatedAt = DateTime.UtcNow;
    }

    // NDR Settings
    public void UpdateNdrSettings(
        bool autoAssignNdrToAgent,
        Guid? defaultNdrAgentId,
        int ndrFollowUpIntervalHours,
        int maxNdrAttempts,
        bool escalateAfterMaxAttempts)
    {
        AutoAssignNdrToAgent = autoAssignNdrToAgent;
        DefaultNdrAgentId = defaultNdrAgentId;
        NdrFollowUpIntervalHours = Math.Max(1, ndrFollowUpIntervalHours);
        MaxNdrAttempts = Math.Max(1, maxNdrAttempts);
        EscalateAfterMaxAttempts = escalateAfterMaxAttempts;
        UpdatedAt = DateTime.UtcNow;
    }

    // Notification Settings
    public void UpdateNotificationSettings(
        bool sendOrderConfirmationEmail,
        bool sendOrderConfirmationSms,
        bool sendShipmentNotification,
        bool sendDeliveryNotification,
        bool sendNdrNotification,
        bool sendRtoNotification)
    {
        SendOrderConfirmationEmail = sendOrderConfirmationEmail;
        SendOrderConfirmationSms = sendOrderConfirmationSms;
        SendShipmentNotification = sendShipmentNotification;
        SendDeliveryNotification = sendDeliveryNotification;
        SendNdrNotification = sendNdrNotification;
        SendRtoNotification = sendRtoNotification;
        UpdatedAt = DateTime.UtcNow;
    }

    // Inventory Settings
    public void UpdateInventorySettings(
        int lowStockThreshold,
        bool alertOnLowStock,
        bool alertOnOutOfStock,
        bool preventOverselling)
    {
        LowStockThreshold = Math.Max(0, lowStockThreshold);
        AlertOnLowStock = alertOnLowStock;
        AlertOnOutOfStock = alertOnOutOfStock;
        PreventOverselling = preventOverselling;
        UpdatedAt = DateTime.UtcNow;
    }

    // Sync Settings
    public void UpdateSyncSettings(
        bool autoSyncOrders,
        int orderSyncIntervalMinutes,
        bool autoSyncInventory,
        int inventorySyncIntervalMinutes)
    {
        AutoSyncOrders = autoSyncOrders;
        OrderSyncIntervalMinutes = Math.Max(5, orderSyncIntervalMinutes);
        AutoSyncInventory = autoSyncInventory;
        InventorySyncIntervalMinutes = Math.Max(15, inventorySyncIntervalMinutes);
        UpdatedAt = DateTime.UtcNow;
    }

    // Branding Settings
    public void UpdateBrandingSettings(
        string? primaryColor,
        string? secondaryColor,
        string? invoiceLogoUrl,
        string? invoiceFooterText)
    {
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        InvoiceLogoUrl = invoiceLogoUrl;
        InvoiceFooterText = invoiceFooterText;
        UpdatedAt = DateTime.UtcNow;
    }
}
