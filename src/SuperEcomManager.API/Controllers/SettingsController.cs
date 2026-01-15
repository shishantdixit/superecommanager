using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Settings;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Tenant settings and configuration endpoints.
/// </summary>
[Authorize]
public class SettingsController : ApiControllerBase
{
    /// <summary>
    /// Get all tenant settings.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantSettingsDto>>> GetSettings()
    {
        var result = await Mediator.Send(new GetSettingsQuery());

        if (result.IsFailure)
            return BadRequestResponse<TenantSettingsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update general settings (currency, timezone, date/time formats).
    /// </summary>
    [HttpPut("general")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantSettingsDto>>> UpdateGeneralSettings(
        [FromBody] UpdateGeneralSettingsDto request)
    {
        var result = await Mediator.Send(new UpdateGeneralSettingsCommand
        {
            Currency = request.Currency,
            Timezone = request.Timezone,
            DateFormat = request.DateFormat,
            TimeFormat = request.TimeFormat
        });

        if (result.IsFailure)
            return BadRequestResponse<TenantSettingsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update order processing settings.
    /// </summary>
    [HttpPut("orders")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantSettingsDto>>> UpdateOrderSettings(
        [FromBody] UpdateOrderSettingsDto request)
    {
        var result = await Mediator.Send(new UpdateOrderSettingsCommand
        {
            AutoConfirmOrders = request.AutoConfirmOrders,
            AutoAssignToDefaultCourier = request.AutoAssignToDefaultCourier,
            DefaultCourierAccountId = request.DefaultCourierAccountId,
            OrderProcessingCutoffHour = request.OrderProcessingCutoffHour,
            EnableCOD = request.EnableCOD,
            MaxCODAmount = request.MaxCODAmount
        });

        if (result.IsFailure)
            return BadRequestResponse<TenantSettingsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update shipment settings.
    /// </summary>
    [HttpPut("shipments")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantSettingsDto>>> UpdateShipmentSettings(
        [FromBody] UpdateShipmentSettingsDto request)
    {
        var result = await Mediator.Send(new UpdateShipmentSettingsCommand
        {
            AutoCreateShipment = request.AutoCreateShipment,
            RestockOnRTO = request.RestockOnRTO,
            DefaultPackageWeight = request.DefaultPackageWeight,
            DefaultPackageLength = request.DefaultPackageLength,
            DefaultPackageWidth = request.DefaultPackageWidth,
            DefaultPackageHeight = request.DefaultPackageHeight
        });

        if (result.IsFailure)
            return BadRequestResponse<TenantSettingsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update NDR handling settings.
    /// </summary>
    [HttpPut("ndr")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantSettingsDto>>> UpdateNdrSettings(
        [FromBody] UpdateNdrSettingsDto request)
    {
        var result = await Mediator.Send(new UpdateNdrSettingsCommand
        {
            AutoAssignNdrToAgent = request.AutoAssignNdrToAgent,
            DefaultNdrAgentId = request.DefaultNdrAgentId,
            NdrFollowUpIntervalHours = request.NdrFollowUpIntervalHours,
            MaxNdrAttempts = request.MaxNdrAttempts,
            EscalateAfterMaxAttempts = request.EscalateAfterMaxAttempts
        });

        if (result.IsFailure)
            return BadRequestResponse<TenantSettingsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update notification preferences.
    /// </summary>
    [HttpPut("notifications")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantSettingsDto>>> UpdateNotificationSettings(
        [FromBody] UpdateNotificationSettingsDto request)
    {
        var result = await Mediator.Send(new UpdateNotificationSettingsCommand
        {
            SendOrderConfirmationEmail = request.SendOrderConfirmationEmail,
            SendOrderConfirmationSms = request.SendOrderConfirmationSms,
            SendShipmentNotification = request.SendShipmentNotification,
            SendDeliveryNotification = request.SendDeliveryNotification,
            SendNdrNotification = request.SendNdrNotification,
            SendRtoNotification = request.SendRtoNotification
        });

        if (result.IsFailure)
            return BadRequestResponse<TenantSettingsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update inventory settings.
    /// </summary>
    [HttpPut("inventory")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantSettingsDto>>> UpdateInventorySettings(
        [FromBody] UpdateInventorySettingsDto request)
    {
        var result = await Mediator.Send(new UpdateInventorySettingsCommand
        {
            LowStockThreshold = request.LowStockThreshold,
            AlertOnLowStock = request.AlertOnLowStock,
            AlertOnOutOfStock = request.AlertOnOutOfStock,
            PreventOverselling = request.PreventOverselling
        });

        if (result.IsFailure)
            return BadRequestResponse<TenantSettingsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update sync/integration settings.
    /// </summary>
    [HttpPut("sync")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantSettingsDto>>> UpdateSyncSettings(
        [FromBody] UpdateSyncSettingsDto request)
    {
        var result = await Mediator.Send(new UpdateSyncSettingsCommand
        {
            AutoSyncOrders = request.AutoSyncOrders,
            OrderSyncIntervalMinutes = request.OrderSyncIntervalMinutes,
            AutoSyncInventory = request.AutoSyncInventory,
            InventorySyncIntervalMinutes = request.InventorySyncIntervalMinutes
        });

        if (result.IsFailure)
            return BadRequestResponse<TenantSettingsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Update branding settings.
    /// </summary>
    [HttpPut("branding")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantSettingsDto>>> UpdateBrandingSettings(
        [FromBody] UpdateBrandingSettingsDto request)
    {
        var result = await Mediator.Send(new UpdateBrandingSettingsCommand
        {
            PrimaryColor = request.PrimaryColor,
            SecondaryColor = request.SecondaryColor,
            InvoiceLogoUrl = request.InvoiceLogoUrl,
            InvoiceFooterText = request.InvoiceFooterText
        });

        if (result.IsFailure)
            return BadRequestResponse<TenantSettingsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }
}
