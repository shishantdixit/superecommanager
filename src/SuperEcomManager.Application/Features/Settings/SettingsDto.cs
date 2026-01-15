namespace SuperEcomManager.Application.Features.Settings;

/// <summary>
/// Complete tenant settings DTO.
/// </summary>
public record TenantSettingsDto
{
    public Guid Id { get; init; }
    public GeneralSettingsDto General { get; init; } = new();
    public OrderSettingsDto Orders { get; init; } = new();
    public ShipmentSettingsDto Shipments { get; init; } = new();
    public NdrSettingsDto Ndr { get; init; } = new();
    public NotificationSettingsDto Notifications { get; init; } = new();
    public InventorySettingsDto Inventory { get; init; } = new();
    public SyncSettingsDto Sync { get; init; } = new();
    public BrandingSettingsDto Branding { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// General settings DTO.
/// </summary>
public record GeneralSettingsDto
{
    public string Currency { get; init; } = "INR";
    public string Timezone { get; init; } = "Asia/Kolkata";
    public string DateFormat { get; init; } = "dd/MM/yyyy";
    public string TimeFormat { get; init; } = "HH:mm";
}

/// <summary>
/// Order settings DTO.
/// </summary>
public record OrderSettingsDto
{
    public bool AutoConfirmOrders { get; init; }
    public bool AutoAssignToDefaultCourier { get; init; }
    public Guid? DefaultCourierAccountId { get; init; }
    public string? DefaultCourierName { get; init; }
    public int OrderProcessingCutoffHour { get; init; } = 18;
    public bool EnableCOD { get; init; } = true;
    public decimal? MaxCODAmount { get; init; }
}

/// <summary>
/// Shipment settings DTO.
/// </summary>
public record ShipmentSettingsDto
{
    public bool AutoCreateShipment { get; init; }
    public bool RestockOnRTO { get; init; } = true;
    public int DefaultPackageWeight { get; init; } = 500;
    public int DefaultPackageLength { get; init; } = 20;
    public int DefaultPackageWidth { get; init; } = 15;
    public int DefaultPackageHeight { get; init; } = 10;
}

/// <summary>
/// NDR settings DTO.
/// </summary>
public record NdrSettingsDto
{
    public bool AutoAssignNdrToAgent { get; init; }
    public Guid? DefaultNdrAgentId { get; init; }
    public string? DefaultNdrAgentName { get; init; }
    public int NdrFollowUpIntervalHours { get; init; } = 24;
    public int MaxNdrAttempts { get; init; } = 3;
    public bool EscalateAfterMaxAttempts { get; init; } = true;
}

/// <summary>
/// Notification settings DTO.
/// </summary>
public record NotificationSettingsDto
{
    public bool SendOrderConfirmationEmail { get; init; } = true;
    public bool SendOrderConfirmationSms { get; init; }
    public bool SendShipmentNotification { get; init; } = true;
    public bool SendDeliveryNotification { get; init; } = true;
    public bool SendNdrNotification { get; init; } = true;
    public bool SendRtoNotification { get; init; } = true;
}

/// <summary>
/// Inventory settings DTO.
/// </summary>
public record InventorySettingsDto
{
    public int LowStockThreshold { get; init; } = 10;
    public bool AlertOnLowStock { get; init; } = true;
    public bool AlertOnOutOfStock { get; init; } = true;
    public bool PreventOverselling { get; init; } = true;
}

/// <summary>
/// Sync settings DTO.
/// </summary>
public record SyncSettingsDto
{
    public bool AutoSyncOrders { get; init; } = true;
    public int OrderSyncIntervalMinutes { get; init; } = 15;
    public bool AutoSyncInventory { get; init; }
    public int InventorySyncIntervalMinutes { get; init; } = 60;
}

/// <summary>
/// Branding settings DTO.
/// </summary>
public record BrandingSettingsDto
{
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? InvoiceLogoUrl { get; init; }
    public string? InvoiceFooterText { get; init; }
}

// Update Request DTOs

/// <summary>
/// Request DTO for updating general settings.
/// </summary>
public record UpdateGeneralSettingsDto
{
    public string Currency { get; init; } = "INR";
    public string Timezone { get; init; } = "Asia/Kolkata";
    public string DateFormat { get; init; } = "dd/MM/yyyy";
    public string TimeFormat { get; init; } = "HH:mm";
}

/// <summary>
/// Request DTO for updating order settings.
/// </summary>
public record UpdateOrderSettingsDto
{
    public bool AutoConfirmOrders { get; init; }
    public bool AutoAssignToDefaultCourier { get; init; }
    public Guid? DefaultCourierAccountId { get; init; }
    public int OrderProcessingCutoffHour { get; init; } = 18;
    public bool EnableCOD { get; init; } = true;
    public decimal? MaxCODAmount { get; init; }
}

/// <summary>
/// Request DTO for updating shipment settings.
/// </summary>
public record UpdateShipmentSettingsDto
{
    public bool AutoCreateShipment { get; init; }
    public bool RestockOnRTO { get; init; } = true;
    public int DefaultPackageWeight { get; init; } = 500;
    public int DefaultPackageLength { get; init; } = 20;
    public int DefaultPackageWidth { get; init; } = 15;
    public int DefaultPackageHeight { get; init; } = 10;
}

/// <summary>
/// Request DTO for updating NDR settings.
/// </summary>
public record UpdateNdrSettingsDto
{
    public bool AutoAssignNdrToAgent { get; init; }
    public Guid? DefaultNdrAgentId { get; init; }
    public int NdrFollowUpIntervalHours { get; init; } = 24;
    public int MaxNdrAttempts { get; init; } = 3;
    public bool EscalateAfterMaxAttempts { get; init; } = true;
}

/// <summary>
/// Request DTO for updating notification settings.
/// </summary>
public record UpdateNotificationSettingsDto
{
    public bool SendOrderConfirmationEmail { get; init; } = true;
    public bool SendOrderConfirmationSms { get; init; }
    public bool SendShipmentNotification { get; init; } = true;
    public bool SendDeliveryNotification { get; init; } = true;
    public bool SendNdrNotification { get; init; } = true;
    public bool SendRtoNotification { get; init; } = true;
}

/// <summary>
/// Request DTO for updating inventory settings.
/// </summary>
public record UpdateInventorySettingsDto
{
    public int LowStockThreshold { get; init; } = 10;
    public bool AlertOnLowStock { get; init; } = true;
    public bool AlertOnOutOfStock { get; init; } = true;
    public bool PreventOverselling { get; init; } = true;
}

/// <summary>
/// Request DTO for updating sync settings.
/// </summary>
public record UpdateSyncSettingsDto
{
    public bool AutoSyncOrders { get; init; } = true;
    public int OrderSyncIntervalMinutes { get; init; } = 15;
    public bool AutoSyncInventory { get; init; }
    public int InventorySyncIntervalMinutes { get; init; } = 60;
}

/// <summary>
/// Request DTO for updating branding settings.
/// </summary>
public record UpdateBrandingSettingsDto
{
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? InvoiceLogoUrl { get; init; }
    public string? InvoiceFooterText { get; init; }
}
