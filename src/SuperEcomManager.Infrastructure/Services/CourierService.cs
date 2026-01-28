using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers;
using System.Text.Json;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Service for interacting with courier APIs via adapters.
/// </summary>
public class CourierService : ICourierService
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICourierAdapterFactory _courierAdapterFactory;
    private readonly ILogger<CourierService> _logger;

    public CourierService(
        ITenantDbContext dbContext,
        ICourierAdapterFactory courierAdapterFactory,
        ILogger<CourierService> logger)
    {
        _dbContext = dbContext;
        _courierAdapterFactory = courierAdapterFactory;
        _logger = logger;
    }

    public async Task<CourierConnectionResult> TestConnectionAsync(
        Guid courierAccountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == courierAccountId && c.DeletedAt == null, cancellationToken);

        if (account == null)
        {
            return CourierConnectionResult.Failed("Courier account not found");
        }

        if (string.IsNullOrEmpty(account.ApiKey) && string.IsNullOrEmpty(account.AccessToken))
        {
            return CourierConnectionResult.Failed("No credentials configured", account.Name);
        }

        var adapter = _courierAdapterFactory.GetAdapter(account.CourierType);
        if (adapter == null)
        {
            return CourierConnectionResult.Failed(
                $"No adapter available for {account.CourierType}", account.Name);
        }

        var credentials = new CourierCredentials
        {
            ApiKey = account.ApiKey,
            ApiSecret = account.ApiSecret,
            AccessToken = account.AccessToken,
            ChannelId = account.ChannelId,
            AdditionalSettings = string.IsNullOrEmpty(account.SettingsJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(account.SettingsJson)
                  ?? new Dictionary<string, string>()
        };

        var result = await adapter.ValidateCredentialsAsync(credentials, cancellationToken);

        if (result.IsSuccess)
        {
            account.MarkConnected();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Connection test successful for {AccountName} ({CourierType})",
                account.Name, account.CourierType);

            return CourierConnectionResult.Connected(account.Name);
        }

        account.MarkDisconnected(result.ErrorMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Connection test failed for {AccountName}: {Error}",
            account.Name, result.ErrorMessage);

        return CourierConnectionResult.Failed(
            result.ErrorMessage ?? "Authentication failed", account.Name);
    }

    public async Task<CourierShipmentResult> CreateShipmentAsync(
        Guid orderId,
        CourierType courierType,
        Guid? courierAccountId = null,
        string? serviceCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get order with items
            var order = await _dbContext.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.DeletedAt == null, cancellationToken);

            if (order == null)
            {
                return CourierShipmentResult.Failure("Order not found");
            }

            // Get shipment for the order
            var shipment = await _dbContext.Shipments
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.OrderId == orderId && s.DeletedAt == null, cancellationToken);

            if (shipment == null)
            {
                return CourierShipmentResult.Failure("Shipment not found");
            }

            // Delegate to the overload that accepts entities directly
            return await CreateShipmentAsync(
                shipment,
                order,
                courierAccountId,
                serviceCode,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling courier API for order {OrderId}", orderId);
            return CourierShipmentResult.Failure($"Error communicating with courier: {ex.Message}");
        }
    }

    public async Task<CourierShipmentResult> CreateShipmentAsync(
        Domain.Entities.Shipments.Shipment shipment,
        Domain.Entities.Orders.Order order,
        Guid? courierAccountId = null,
        string? serviceCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Determine courier type
            var courierType = shipment.CourierType;

            // Get courier account/credentials
            var courierAccount = courierAccountId.HasValue
                // Use the specific courier account that was selected
                ? await _dbContext.CourierAccounts
                    .FirstOrDefaultAsync(ca => ca.Id == courierAccountId.Value &&
                                              ca.DeletedAt == null,
                        cancellationToken)
                // Fallback: Find any active courier account for this type
                : await _dbContext.CourierAccounts
                    .FirstOrDefaultAsync(ca => ca.CourierType == courierType &&
                                              ca.IsActive &&
                                              ca.DeletedAt == null,
                        cancellationToken);

            if (courierAccount == null)
            {
                var message = courierAccountId.HasValue
                    ? "Selected courier account not found"
                    : $"No active {courierType} account configured";
                _logger.LogWarning(message);
                return CourierShipmentResult.Failure(message);
            }

            if (!courierAccount.IsActive)
            {
                _logger.LogWarning("Courier account {CourierAccountId} is not active", courierAccount.Id);
                return CourierShipmentResult.Failure("Selected courier account is not active");
            }

            if (!courierAccount.IsConnected)
            {
                _logger.LogWarning("Courier account {CourierAccountId} is not connected", courierAccount.Id);
                return CourierShipmentResult.Failure("Selected courier account is not connected. Please check your courier credentials.");
            }

            // Get the courier adapter
            var adapter = _courierAdapterFactory.GetAdapter(courierType);
            if (adapter == null)
            {
                _logger.LogWarning("No adapter found for courier type {CourierType}", courierType);
                return CourierShipmentResult.Failure($"Courier adapter not available for {courierType}");
            }

            // Build courier credentials
            var credentials = new CourierCredentials
            {
                ApiKey = courierAccount.ApiKey,
                ApiSecret = courierAccount.ApiSecret,
                AccessToken = courierAccount.AccessToken,
                ChannelId = courierAccount.ChannelId,
                AdditionalSettings = string.IsNullOrEmpty(courierAccount.SettingsJson)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(courierAccount.SettingsJson)
                      ?? new Dictionary<string, string>()
            };

            // Build shipment request for courier
            var courierRequest = new ShipmentRequest
            {
                OrderId = order.Id.ToString(),
                OrderNumber = order.OrderNumber,
                PickupName = shipment.PickupAddress.Name,
                PickupPhone = shipment.PickupAddress.Phone ?? "",
                PickupAddress = shipment.PickupAddress.Line1,
                PickupCity = shipment.PickupAddress.City,
                PickupState = shipment.PickupAddress.State,
                PickupPincode = shipment.PickupAddress.PostalCode,
                DeliveryName = shipment.DeliveryAddress.Name,
                DeliveryPhone = shipment.DeliveryAddress.Phone ?? "",
                DeliveryAddress = $"{shipment.DeliveryAddress.Line1} {shipment.DeliveryAddress.Line2}".Trim(),
                DeliveryCity = shipment.DeliveryAddress.City,
                DeliveryState = shipment.DeliveryAddress.State,
                DeliveryPincode = shipment.DeliveryAddress.PostalCode,
                Weight = 0.5m, // Default weight - should be passed from caller
                Length = 10,
                Width = 10,
                Height = 10,
                IsCOD = order.IsCOD,
                CODAmount = order.IsCOD ? order.TotalAmount.Amount : null,
                DeclaredValue = order.TotalAmount.Amount,
                ServiceCode = serviceCode,
                Items = shipment.Items.Select(i => new ShipmentItemRequest
                {
                    Sku = i.Sku,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = 0 // Would need to fetch from order items if needed
                }).ToList()
            };

            // Call courier API to create shipment
            var result = await adapter.CreateShipmentAsync(credentials, courierRequest, cancellationToken);

            if (result.IsSuccess && result.Data != null)
            {
                // Check for partial success (order created but AWB assignment failed)
                if (result.Data.IsPartialSuccess)
                {
                    _logger.LogWarning(
                        "Shipment for order {OrderId} partially created in {Courier}. Order ID: {ExternalOrderId}, Shipment ID: {ExternalShipmentId}. AWB Error: {AwbError}",
                        order.Id, adapter.DisplayName, result.Data.ExternalOrderId, result.Data.ExternalShipmentId, result.Data.AwbError);

                    return CourierShipmentResult.PartialSuccess(
                        result.Data.ExternalOrderId ?? string.Empty,
                        result.Data.ExternalShipmentId,
                        result.Data.AwbError ?? "Failed to assign courier");
                }

                _logger.LogInformation(
                    "Shipment for order {OrderId} successfully created in {Courier} with AWB {AWB}",
                    order.Id, adapter.DisplayName, result.Data.AwbNumber);

                return CourierShipmentResult.Ok(
                    result.Data.AwbNumber,
                    result.Data.CourierName ?? adapter.DisplayName,
                    result.Data.TrackingUrl,
                    result.Data.LabelUrl,
                    result.Data.ExternalOrderId,
                    result.Data.ExternalShipmentId);
            }

            _logger.LogWarning(
                "Failed to create shipment in {Courier}: {Error}",
                adapter.DisplayName, result.ErrorMessage);

            return CourierShipmentResult.Failure(result.ErrorMessage ?? "Failed to create shipment with courier");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling courier API for shipment {ShipmentNumber}, order {OrderId}",
                shipment.ShipmentNumber, order.Id);
            return CourierShipmentResult.Failure($"Error communicating with courier: {ex.Message}");
        }
    }
}
