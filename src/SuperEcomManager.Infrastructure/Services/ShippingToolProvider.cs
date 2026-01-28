using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Tool provider for shipping-related operations.
/// Currently supports Shiprocket, extensible for other couriers.
/// </summary>
public class ShippingToolProvider : IChatToolProvider
{
    private readonly ITenantDbContext _dbContext;
    private readonly IShiprocketChannelService _shiprocketService;
    private readonly ILogger<ShippingToolProvider> _logger;

    private readonly List<ChatTool> _tools;

    public string Category => "Shipping";
    public int Priority => 10;

    public ShippingToolProvider(
        ITenantDbContext dbContext,
        IShiprocketChannelService shiprocketService,
        ILogger<ShippingToolProvider> logger)
    {
        _dbContext = dbContext;
        _shiprocketService = shiprocketService;
        _logger = logger;

        _tools = InitializeTools();
    }

    private List<ChatTool> InitializeTools()
    {
        return new List<ChatTool>
        {
            new ChatTool
            {
                Name = "track_shipment",
                Description = "Track a shipment by AWB number or shipment ID. Returns current status, location, and tracking history.",
                Category = "Shipping",
                InputSchema = JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        awb_number = new { type = "string", description = "The AWB (Air Waybill) number to track" },
                        shipment_id = new { type = "string", description = "The shipment ID to track (alternative to AWB)" }
                    },
                    required = new[] { "awb_number" }
                })
            },
            new ChatTool
            {
                Name = "check_serviceability",
                Description = "Check if a courier can deliver to a specific pincode and get available courier rates.",
                Category = "Shipping",
                InputSchema = JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        pickup_pincode = new { type = "string", description = "The pickup location pincode" },
                        delivery_pincode = new { type = "string", description = "The delivery location pincode" },
                        weight = new { type = "number", description = "Package weight in kilograms" },
                        is_cod = new { type = "boolean", description = "Whether this is a Cash on Delivery shipment" }
                    },
                    required = new[] { "pickup_pincode", "delivery_pincode", "weight" }
                })
            },
            new ChatTool
            {
                Name = "get_shipment_details",
                Description = "Get detailed information about a shipment including order details, addresses, and status.",
                Category = "Shipping",
                InputSchema = JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        shipment_id = new { type = "string", description = "The shipment ID to look up" },
                        awb_number = new { type = "string", description = "The AWB number to look up (alternative)" }
                    }
                })
            },
            new ChatTool
            {
                Name = "list_recent_shipments",
                Description = "List recent shipments with optional status filter.",
                Category = "Shipping",
                InputSchema = JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        status = new { type = "string", description = "Filter by status: Created, InTransit, OutForDelivery, Delivered, NDR, RTO" },
                        limit = new { type = "integer", description = "Maximum number of shipments to return (default: 10)" }
                    }
                })
            },
            new ChatTool
            {
                Name = "get_shipment_stats",
                Description = "Get shipping statistics and summary for the tenant.",
                Category = "Shipping",
                InputSchema = JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        days = new { type = "integer", description = "Number of days to include in stats (default: 30)" }
                    }
                })
            }
        };
    }

    public IReadOnlyList<ChatTool> GetTools() => _tools.AsReadOnly();

    public bool CanHandle(string toolName)
    {
        return _tools.Any(t => t.Name == toolName);
    }

    public async Task<ChatToolResult> ExecuteToolAsync(
        ChatToolCall toolCall,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var args = JsonSerializer.Deserialize<JsonElement>(toolCall.Arguments);

            var result = toolCall.Name switch
            {
                "track_shipment" => await TrackShipmentAsync(args, context, cancellationToken),
                "check_serviceability" => await CheckServiceabilityAsync(args, context, cancellationToken),
                "get_shipment_details" => await GetShipmentDetailsAsync(args, context, cancellationToken),
                "list_recent_shipments" => await ListRecentShipmentsAsync(args, context, cancellationToken),
                "get_shipment_stats" => await GetShipmentStatsAsync(args, context, cancellationToken),
                _ => $"Unknown tool: {toolCall.Name}"
            };

            return new ChatToolResult
            {
                ToolCallId = toolCall.Id,
                ToolName = toolCall.Name,
                Content = result,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing shipping tool {ToolName}", toolCall.Name);

            return new ChatToolResult
            {
                ToolCallId = toolCall.Id,
                ToolName = toolCall.Name,
                Content = $"Error: {ex.Message}",
                IsSuccess = false,
                Error = ex.Message
            };
        }
    }

    private async Task<string> TrackShipmentAsync(
        JsonElement args,
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        var awbNumber = args.TryGetProperty("awb_number", out var awb) ? awb.GetString() : null;
        var shipmentIdStr = args.TryGetProperty("shipment_id", out var sid) ? sid.GetString() : null;

        var query = _dbContext.Shipments.AsQueryable();

        if (!string.IsNullOrEmpty(awbNumber))
        {
            query = query.Where(s => s.AwbNumber == awbNumber);
        }
        else if (!string.IsNullOrEmpty(shipmentIdStr) && Guid.TryParse(shipmentIdStr, out var shipmentId))
        {
            query = query.Where(s => s.Id == shipmentId);
        }
        else
        {
            return "Please provide either an AWB number or shipment ID to track.";
        }

        var shipment = await query
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(cancellationToken);

        if (shipment == null)
        {
            return $"No shipment found with AWB: {awbNumber ?? shipmentIdStr}";
        }

        var trackingHistory = shipment.TrackingEvents
            .OrderByDescending(t => t.EventTime)
            .Take(5)
            .Select(t => $"- {t.EventTime:MMM dd, HH:mm}: {t.Status} - {t.Location ?? "Unknown location"}")
            .ToList();

        return $@"**Shipment Tracking**
AWB: {shipment.AwbNumber ?? "Not assigned"}
Status: {shipment.Status}
Courier: {shipment.CourierName ?? "Not assigned"}
Created: {shipment.CreatedAt:MMM dd, yyyy}
{(shipment.ExpectedDeliveryDate.HasValue ? $"Expected Delivery: {shipment.ExpectedDeliveryDate:MMM dd, yyyy}" : "")}

**Recent Tracking Updates:**
{(trackingHistory.Any() ? string.Join("\n", trackingHistory) : "No tracking updates available")}";
    }

    private async Task<string> CheckServiceabilityAsync(
        JsonElement args,
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        var pickupPincode = args.GetProperty("pickup_pincode").GetString()!;
        var deliveryPincode = args.GetProperty("delivery_pincode").GetString()!;
        var weight = args.GetProperty("weight").GetDecimal();
        var isCod = args.TryGetProperty("is_cod", out var cod) && cod.GetBoolean();

        // Get default courier account
        var courierAccount = await _dbContext.CourierAccounts
            .Where(ca => ca.CourierType == CourierType.Shiprocket && ca.IsActive && ca.DeletedAt == null)
            .OrderByDescending(ca => ca.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

        if (courierAccount == null)
        {
            return "No active Shiprocket courier account configured. Please set up a courier account first.";
        }

        var result = await _shiprocketService.CheckServiceabilityAsync(
            courierAccount.Id,
            pickupPincode,
            deliveryPincode,
            weight,
            isCod,
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            return $"Serviceability check failed: {result.ErrorMessage}";
        }

        if (result.AvailableCouriers == null || !result.AvailableCouriers.Any())
        {
            return $"No couriers available for delivery from {pickupPincode} to {deliveryPincode}.";
        }

        var couriers = result.AvailableCouriers
            .OrderBy(c => c.FreightCharge)
            .Take(5)
            .Select(c =>
            {
                var recommended = c.CourierId == result.RecommendedCourierId ? " ⭐ Recommended" : "";
                return $"- **{c.CourierName}**: ₹{c.FreightCharge:N2} | ETA: {c.EstimatedDeliveryDays ?? "N/A"} | Rating: {c.Rating:N1}{recommended}";
            })
            .ToList();

        return $@"**Courier Serviceability: {pickupPincode} → {deliveryPincode}**
Weight: {weight} kg | COD: {(isCod ? "Yes" : "No")}

**Available Couriers ({result.AvailableCouriers.Count} total):**
{string.Join("\n", couriers)}

{(result.AvailableCouriers.Count > 5 ? $"...and {result.AvailableCouriers.Count - 5} more options available." : "")}";
    }

    private async Task<string> GetShipmentDetailsAsync(
        JsonElement args,
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        var shipmentIdStr = args.TryGetProperty("shipment_id", out var sid) ? sid.GetString() : null;
        var awbNumber = args.TryGetProperty("awb_number", out var awb) ? awb.GetString() : null;

        var query = _dbContext.Shipments
            .Include(s => s.Items)
            .AsQueryable();

        if (!string.IsNullOrEmpty(shipmentIdStr) && Guid.TryParse(shipmentIdStr, out var shipmentId))
        {
            query = query.Where(s => s.Id == shipmentId);
        }
        else if (!string.IsNullOrEmpty(awbNumber))
        {
            query = query.Where(s => s.AwbNumber == awbNumber);
        }
        else
        {
            return "Please provide a shipment ID or AWB number.";
        }

        var shipment = await query.FirstOrDefaultAsync(cancellationToken);

        if (shipment == null)
        {
            return "Shipment not found.";
        }

        // Load order separately via OrderId
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        var items = shipment.Items
            .Select(i => $"- {i.Name} x {i.Quantity}")
            .ToList();

        var weight = shipment.Dimensions?.WeightKg ?? 0;
        var dimensions = shipment.Dimensions != null
            ? $"{shipment.Dimensions.LengthCm}x{shipment.Dimensions.WidthCm}x{shipment.Dimensions.HeightCm} cm"
            : "N/A";
        var codAmount = shipment.CODAmount?.Amount ?? 0;
        var shippingCost = shipment.ShippingCost?.Amount ?? 0;

        return $@"**Shipment Details**
ID: {shipment.Id}
AWB: {shipment.AwbNumber ?? "Not assigned"}
Status: {shipment.Status}
Courier: {shipment.CourierName ?? "Not assigned"}

**Order Info:**
Order Number: {order?.OrderNumber ?? "N/A"}
Customer: {order?.CustomerName ?? "N/A"}

**Package:**
Weight: {weight} kg
Dimensions: {dimensions}
COD: {(shipment.IsCOD ? $"Yes (₹{codAmount:N2})" : "No")}
Shipping Cost: ₹{shippingCost:N2}

**Items ({shipment.Items.Count}):**
{(items.Any() ? string.Join("\n", items) : "No items listed")}

**Delivery Address:**
{shipment.DeliveryAddress?.Line1 ?? ""}
{shipment.DeliveryAddress?.City ?? ""}, {shipment.DeliveryAddress?.State ?? ""} {shipment.DeliveryAddress?.PostalCode ?? ""}";
    }

    private async Task<string> ListRecentShipmentsAsync(
        JsonElement args,
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        var statusStr = args.TryGetProperty("status", out var s) ? s.GetString() : null;
        var limit = args.TryGetProperty("limit", out var l) ? l.GetInt32() : 10;

        limit = Math.Min(limit, 20); // Cap at 20

        var query = _dbContext.Shipments
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<ShipmentStatus>(statusStr, true, out var status))
        {
            query = query.Where(s => s.Status == status);
        }

        var shipments = await query.Take(limit).ToListAsync(cancellationToken);

        if (!shipments.Any())
        {
            return statusStr != null
                ? $"No shipments found with status: {statusStr}"
                : "No shipments found.";
        }

        // Load orders for shipments
        var orderIds = shipments.Select(s => s.OrderId).Distinct().ToList();
        var orders = await _dbContext.Orders
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var shipmentList = shipments.Select(s =>
        {
            var customerName = orders.TryGetValue(s.OrderId, out var order) ? order.CustomerName : "N/A";
            return $"- **{s.AwbNumber ?? s.Id.ToString()[..8]}** | {s.Status} | {customerName} | {s.CreatedAt:MMM dd}";
        }).ToList();

        return $@"**Recent Shipments** ({shipments.Count})
{string.Join("\n", shipmentList)}";
    }

    private async Task<string> GetShipmentStatsAsync(
        JsonElement args,
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        var days = args.TryGetProperty("days", out var d) ? d.GetInt32() : 30;
        days = Math.Min(days, 90); // Cap at 90 days

        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        var shipments = await _dbContext.Shipments
            .Where(s => s.CreatedAt >= cutoffDate)
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var total = shipments.Sum(s => s.Count);
        var delivered = shipments.FirstOrDefault(s => s.Status == ShipmentStatus.Delivered)?.Count ?? 0;
        var inTransit = shipments.FirstOrDefault(s => s.Status == ShipmentStatus.InTransit)?.Count ?? 0;
        var ndr = shipments.FirstOrDefault(s => s.Status == ShipmentStatus.DeliveryFailed)?.Count ?? 0;
        var rto = shipments.FirstOrDefault(s => s.Status == ShipmentStatus.RTOInitiated || s.Status == ShipmentStatus.RTODelivered)?.Count ?? 0;

        var deliveryRate = total > 0 ? (delivered * 100.0 / total) : 0;

        var statusBreakdown = shipments
            .OrderByDescending(s => s.Count)
            .Select(s => $"- {s.Status}: {s.Count}")
            .ToList();

        return $@"**Shipping Statistics (Last {days} Days)**

**Summary:**
- Total Shipments: {total}
- Delivered: {delivered}
- In Transit: {inTransit}
- NDR Cases: {ndr}
- RTO: {rto}

**Delivery Rate:** {deliveryRate:N1}%

**Status Breakdown:**
{string.Join("\n", statusBreakdown)}";
    }
}
