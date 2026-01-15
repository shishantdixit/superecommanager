using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Settings;

namespace SuperEcomManager.Application.Features.Settings;

/// <summary>
/// Command to update general settings.
/// </summary>
[RequirePermission("settings.edit")]
[RequireFeature("settings")]
public record UpdateGeneralSettingsCommand : IRequest<Result<TenantSettingsDto>>, ITenantRequest
{
    public string Currency { get; init; } = "INR";
    public string Timezone { get; init; } = "Asia/Kolkata";
    public string DateFormat { get; init; } = "dd/MM/yyyy";
    public string TimeFormat { get; init; } = "HH:mm";
}

public class UpdateGeneralSettingsCommandHandler : IRequestHandler<UpdateGeneralSettingsCommand, Result<TenantSettingsDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateGeneralSettingsCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        UpdateGeneralSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettings(cancellationToken);

        settings.UpdateGeneralSettings(
            request.Currency,
            request.Timezone,
            request.DateFormat,
            request.TimeFormat);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSettingsQuery(), cancellationToken);
    }

    private async Task<TenantSettings> GetOrCreateSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = TenantSettings.CreateDefault();
            _dbContext.TenantSettings.Add(settings);
        }
        return settings;
    }
}

/// <summary>
/// Command to update order settings.
/// </summary>
[RequirePermission("settings.edit")]
[RequireFeature("settings")]
public record UpdateOrderSettingsCommand : IRequest<Result<TenantSettingsDto>>, ITenantRequest
{
    public bool AutoConfirmOrders { get; init; }
    public bool AutoAssignToDefaultCourier { get; init; }
    public Guid? DefaultCourierAccountId { get; init; }
    public int OrderProcessingCutoffHour { get; init; } = 18;
    public bool EnableCOD { get; init; } = true;
    public decimal? MaxCODAmount { get; init; }
}

public class UpdateOrderSettingsCommandHandler : IRequestHandler<UpdateOrderSettingsCommand, Result<TenantSettingsDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateOrderSettingsCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        UpdateOrderSettingsCommand request,
        CancellationToken cancellationToken)
    {
        // Validate courier account if specified
        if (request.DefaultCourierAccountId.HasValue)
        {
            var courierExists = await _dbContext.CourierAccounts
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.DefaultCourierAccountId.Value, cancellationToken);

            if (!courierExists)
            {
                return Result<TenantSettingsDto>.Failure("Invalid courier account specified.");
            }
        }

        var settings = await GetOrCreateSettings(cancellationToken);

        settings.UpdateOrderSettings(
            request.AutoConfirmOrders,
            request.AutoAssignToDefaultCourier,
            request.DefaultCourierAccountId,
            request.OrderProcessingCutoffHour,
            request.EnableCOD,
            request.MaxCODAmount);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSettingsQuery(), cancellationToken);
    }

    private async Task<TenantSettings> GetOrCreateSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = TenantSettings.CreateDefault();
            _dbContext.TenantSettings.Add(settings);
        }
        return settings;
    }
}

/// <summary>
/// Command to update shipment settings.
/// </summary>
[RequirePermission("settings.edit")]
[RequireFeature("settings")]
public record UpdateShipmentSettingsCommand : IRequest<Result<TenantSettingsDto>>, ITenantRequest
{
    public bool AutoCreateShipment { get; init; }
    public bool RestockOnRTO { get; init; } = true;
    public int DefaultPackageWeight { get; init; } = 500;
    public int DefaultPackageLength { get; init; } = 20;
    public int DefaultPackageWidth { get; init; } = 15;
    public int DefaultPackageHeight { get; init; } = 10;
}

public class UpdateShipmentSettingsCommandHandler : IRequestHandler<UpdateShipmentSettingsCommand, Result<TenantSettingsDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateShipmentSettingsCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        UpdateShipmentSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettings(cancellationToken);

        settings.UpdateShipmentSettings(
            request.AutoCreateShipment,
            request.RestockOnRTO,
            request.DefaultPackageWeight,
            request.DefaultPackageLength,
            request.DefaultPackageWidth,
            request.DefaultPackageHeight);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSettingsQuery(), cancellationToken);
    }

    private async Task<TenantSettings> GetOrCreateSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = TenantSettings.CreateDefault();
            _dbContext.TenantSettings.Add(settings);
        }
        return settings;
    }
}

/// <summary>
/// Command to update NDR settings.
/// </summary>
[RequirePermission("settings.edit")]
[RequireFeature("settings")]
public record UpdateNdrSettingsCommand : IRequest<Result<TenantSettingsDto>>, ITenantRequest
{
    public bool AutoAssignNdrToAgent { get; init; }
    public Guid? DefaultNdrAgentId { get; init; }
    public int NdrFollowUpIntervalHours { get; init; } = 24;
    public int MaxNdrAttempts { get; init; } = 3;
    public bool EscalateAfterMaxAttempts { get; init; } = true;
}

public class UpdateNdrSettingsCommandHandler : IRequestHandler<UpdateNdrSettingsCommand, Result<TenantSettingsDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateNdrSettingsCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        UpdateNdrSettingsCommand request,
        CancellationToken cancellationToken)
    {
        // Validate agent if specified
        if (request.DefaultNdrAgentId.HasValue)
        {
            var agentExists = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.DefaultNdrAgentId.Value && u.DeletedAt == null, cancellationToken);

            if (!agentExists)
            {
                return Result<TenantSettingsDto>.Failure("Invalid NDR agent specified.");
            }
        }

        var settings = await GetOrCreateSettings(cancellationToken);

        settings.UpdateNdrSettings(
            request.AutoAssignNdrToAgent,
            request.DefaultNdrAgentId,
            request.NdrFollowUpIntervalHours,
            request.MaxNdrAttempts,
            request.EscalateAfterMaxAttempts);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSettingsQuery(), cancellationToken);
    }

    private async Task<TenantSettings> GetOrCreateSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = TenantSettings.CreateDefault();
            _dbContext.TenantSettings.Add(settings);
        }
        return settings;
    }
}

/// <summary>
/// Command to update notification settings.
/// </summary>
[RequirePermission("settings.edit")]
[RequireFeature("settings")]
public record UpdateNotificationSettingsCommand : IRequest<Result<TenantSettingsDto>>, ITenantRequest
{
    public bool SendOrderConfirmationEmail { get; init; } = true;
    public bool SendOrderConfirmationSms { get; init; }
    public bool SendShipmentNotification { get; init; } = true;
    public bool SendDeliveryNotification { get; init; } = true;
    public bool SendNdrNotification { get; init; } = true;
    public bool SendRtoNotification { get; init; } = true;
}

public class UpdateNotificationSettingsCommandHandler : IRequestHandler<UpdateNotificationSettingsCommand, Result<TenantSettingsDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateNotificationSettingsCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        UpdateNotificationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettings(cancellationToken);

        settings.UpdateNotificationSettings(
            request.SendOrderConfirmationEmail,
            request.SendOrderConfirmationSms,
            request.SendShipmentNotification,
            request.SendDeliveryNotification,
            request.SendNdrNotification,
            request.SendRtoNotification);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSettingsQuery(), cancellationToken);
    }

    private async Task<TenantSettings> GetOrCreateSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = TenantSettings.CreateDefault();
            _dbContext.TenantSettings.Add(settings);
        }
        return settings;
    }
}

/// <summary>
/// Command to update inventory settings.
/// </summary>
[RequirePermission("settings.edit")]
[RequireFeature("settings")]
public record UpdateInventorySettingsCommand : IRequest<Result<TenantSettingsDto>>, ITenantRequest
{
    public int LowStockThreshold { get; init; } = 10;
    public bool AlertOnLowStock { get; init; } = true;
    public bool AlertOnOutOfStock { get; init; } = true;
    public bool PreventOverselling { get; init; } = true;
}

public class UpdateInventorySettingsCommandHandler : IRequestHandler<UpdateInventorySettingsCommand, Result<TenantSettingsDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateInventorySettingsCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        UpdateInventorySettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettings(cancellationToken);

        settings.UpdateInventorySettings(
            request.LowStockThreshold,
            request.AlertOnLowStock,
            request.AlertOnOutOfStock,
            request.PreventOverselling);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSettingsQuery(), cancellationToken);
    }

    private async Task<TenantSettings> GetOrCreateSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = TenantSettings.CreateDefault();
            _dbContext.TenantSettings.Add(settings);
        }
        return settings;
    }
}

/// <summary>
/// Command to update sync settings.
/// </summary>
[RequirePermission("settings.edit")]
[RequireFeature("settings")]
public record UpdateSyncSettingsCommand : IRequest<Result<TenantSettingsDto>>, ITenantRequest
{
    public bool AutoSyncOrders { get; init; } = true;
    public int OrderSyncIntervalMinutes { get; init; } = 15;
    public bool AutoSyncInventory { get; init; }
    public int InventorySyncIntervalMinutes { get; init; } = 60;
}

public class UpdateSyncSettingsCommandHandler : IRequestHandler<UpdateSyncSettingsCommand, Result<TenantSettingsDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateSyncSettingsCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        UpdateSyncSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettings(cancellationToken);

        settings.UpdateSyncSettings(
            request.AutoSyncOrders,
            request.OrderSyncIntervalMinutes,
            request.AutoSyncInventory,
            request.InventorySyncIntervalMinutes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSettingsQuery(), cancellationToken);
    }

    private async Task<TenantSettings> GetOrCreateSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = TenantSettings.CreateDefault();
            _dbContext.TenantSettings.Add(settings);
        }
        return settings;
    }
}

/// <summary>
/// Command to update branding settings.
/// </summary>
[RequirePermission("settings.edit")]
[RequireFeature("settings")]
public record UpdateBrandingSettingsCommand : IRequest<Result<TenantSettingsDto>>, ITenantRequest
{
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? InvoiceLogoUrl { get; init; }
    public string? InvoiceFooterText { get; init; }
}

public class UpdateBrandingSettingsCommandHandler : IRequestHandler<UpdateBrandingSettingsCommand, Result<TenantSettingsDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateBrandingSettingsCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        UpdateBrandingSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettings(cancellationToken);

        settings.UpdateBrandingSettings(
            request.PrimaryColor,
            request.SecondaryColor,
            request.InvoiceLogoUrl,
            request.InvoiceFooterText);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSettingsQuery(), cancellationToken);
    }

    private async Task<TenantSettings> GetOrCreateSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = TenantSettings.CreateDefault();
            _dbContext.TenantSettings.Add(settings);
        }
        return settings;
    }
}
