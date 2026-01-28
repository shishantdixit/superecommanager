using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.Entities.Orders;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Command to update the status of an order.
/// </summary>
[RequirePermission("orders.edit")]
[RequireFeature("order_management")]
public record UpdateOrderStatusCommand : IRequest<Result<OrderDetailDto>>, ITenantRequest
{
    public Guid OrderId { get; init; }
    public OrderStatus NewStatus { get; init; }
    public string? Reason { get; init; }
}

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result<OrderDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

    public UpdateOrderStatusCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        ILogger<UpdateOrderStatusCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public async Task<Result<OrderDetailDto>> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        // Use execution strategy to support retry policy
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            const int maxRetries = 5;
            int attemptCount = 0;
            var random = new Random();

            while (attemptCount < maxRetries)
            {
                try
                {
                    attemptCount++;

                    // Clear change tracker before loading to ensure fresh data
                    foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                    {
                        entry.State = EntityState.Detached;
                    }

                    // First, verify the order exists
                    var orderExists = await _dbContext.Orders
                        .AsNoTracking()
                        .Where(o => o.Id == request.OrderId && o.DeletedAt == null)
                        .Select(o => new { o.Id, o.Status })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (orderExists == null)
                    {
                        return Result<OrderDetailDto>.Failure("Order not found");
                    }

                    var oldStatus = orderExists.Status;

                    // Use ExecuteUpdateAsync to bypass change tracking
                    var utcNow = DateTime.UtcNow;
                    var userId = _currentUserService.UserId;

                    var rowsAffected = await _dbContext.Orders
                        .Where(o => o.Id == request.OrderId && o.DeletedAt == null)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(o => o.Status, request.NewStatus)
                            .SetProperty(o => o.UpdatedAt, utcNow)
                            .SetProperty(o => o.UpdatedBy, userId)
                            .SetProperty(o => o.ShippedAt, o => request.NewStatus == OrderStatus.Shipped ? (DateTime?)utcNow : o.ShippedAt)
                            .SetProperty(o => o.DeliveredAt, o => request.NewStatus == OrderStatus.Delivered ? (DateTime?)utcNow : o.DeliveredAt)
                            .SetProperty(o => o.CancelledAt, o => request.NewStatus == OrderStatus.Cancelled ? (DateTime?)utcNow : o.CancelledAt)
                            .SetProperty(o => o.FulfillmentStatus, o => request.NewStatus == OrderStatus.Shipped ? FulfillmentStatus.Fulfilled : o.FulfillmentStatus),
                        cancellationToken);

                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("Order {OrderId} was not updated - possibly deleted", request.OrderId);
                        continue; // Retry
                    }

                    // Add status history record using traditional Add (this should work fine as it's a new record)
                    var statusHistory = new OrderStatusHistory(request.OrderId, request.NewStatus, userId, request.Reason);
                    _dbContext.OrderStatusHistory.Add(statusHistory);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Order {OrderId} status changed from {OldStatus} to {NewStatus} by {UserId} (attempt {Attempt})",
                        request.OrderId, oldStatus, request.NewStatus, _currentUserService.UserId, attemptCount);

                    // Reload the order with all navigation properties for the DTO
                    var completeOrder = await _dbContext.Orders
                        .AsNoTracking()
                        .Include(o => o.Channel)
                        .Include(o => o.Items)
                        .Include(o => o.StatusHistory)
                        .FirstAsync(o => o.Id == request.OrderId, cancellationToken);

                    // Get shipment if exists
                    var shipment = await _dbContext.Shipments
                        .AsNoTracking()
                        .Where(s => s.OrderId == request.OrderId && s.DeletedAt == null)
                        .Select(s => new ShipmentSummaryDto
                        {
                            Id = s.Id,
                            AwbNumber = s.AwbNumber,
                            CourierName = s.CourierName ?? s.CourierType.ToString(),
                            CourierType = s.CourierType,
                            Status = s.Status,
                            TrackingUrl = s.TrackingUrl,
                            ExpectedDeliveryDate = s.ExpectedDeliveryDate
                        })
                        .FirstOrDefaultAsync(cancellationToken);

                    var dto = MapToDetailDto(completeOrder, shipment);
                    return Result<OrderDetailDto>.Success(dto);
                }
                catch (DbUpdateConcurrencyException) when (attemptCount < maxRetries)
                {
                    _logger.LogWarning(
                        "Concurrency conflict updating order {OrderId} (attempt {Attempt}/{MaxRetries}). Retrying...",
                        request.OrderId, attemptCount, maxRetries);

                    // Exponential backoff with jitter to avoid thundering herd
                    var baseDelay = Math.Pow(2, attemptCount) * 100; // 200ms, 400ms, 800ms, 1600ms
                    var jitter = random.Next(0, 100); // Random 0-100ms
                    await Task.Delay(TimeSpan.FromMilliseconds(baseDelay + jitter), cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex) when (attemptCount >= maxRetries)
                {
                    _logger.LogError(ex,
                        "Failed to update order {OrderId} after {MaxRetries} attempts due to concurrency conflict",
                        request.OrderId, maxRetries);
                    return Result<OrderDetailDto>.Failure("Unable to update order status due to concurrent modifications. Please try again.");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(
                        "Invalid status transition for order {OrderId}: {Message}",
                        request.OrderId, ex.Message);
                    return Result<OrderDetailDto>.Failure(ex.Message);
                }
            }

            // This should never be reached, but added for completeness
            return Result<OrderDetailDto>.Failure("Unable to update order status. Please try again.");
        });
    }

    private static OrderDetailDto MapToDetailDto(Domain.Entities.Orders.Order order, ShipmentSummaryDto? shipment)
    {
        return new OrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            ExternalOrderId = order.ExternalOrderId,
            ExternalOrderNumber = order.ExternalOrderNumber,
            ChannelId = order.ChannelId,
            ChannelName = order.Channel?.Name ?? "Unknown",
            ChannelType = order.Channel?.Type ?? ChannelType.Custom,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            FulfillmentStatus = order.FulfillmentStatus,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            ShippingAddress = new AddressDto
            {
                Name = order.ShippingAddress.Name,
                Phone = order.ShippingAddress.Phone,
                Line1 = order.ShippingAddress.Line1,
                Line2 = order.ShippingAddress.Line2,
                City = order.ShippingAddress.City,
                State = order.ShippingAddress.State,
                PostalCode = order.ShippingAddress.PostalCode,
                Country = order.ShippingAddress.Country
            },
            BillingAddress = order.BillingAddress != null ? new AddressDto
            {
                Name = order.BillingAddress.Name,
                Phone = order.BillingAddress.Phone,
                Line1 = order.BillingAddress.Line1,
                Line2 = order.BillingAddress.Line2,
                City = order.BillingAddress.City,
                State = order.BillingAddress.State,
                PostalCode = order.BillingAddress.PostalCode,
                Country = order.BillingAddress.Country
            } : null,
            Subtotal = order.Subtotal.Amount,
            DiscountAmount = order.DiscountAmount.Amount,
            TaxAmount = order.TaxAmount.Amount,
            ShippingAmount = order.ShippingAmount.Amount,
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency,
            PaymentMethod = order.PaymentMethod,
            IsCOD = order.IsCOD,
            OrderDate = order.OrderDate,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CustomerNotes = order.CustomerNotes,
            InternalNotes = order.InternalNotes,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductVariantId = i.ProductVariantId,
                Sku = i.Sku,
                Name = i.Name,
                VariantName = i.VariantName,
                Quantity = i.Quantity,
                UnitPrice = new MoneyDto { Amount = i.UnitPrice.Amount, Currency = i.UnitPrice.Currency },
                DiscountAmount = new MoneyDto { Amount = i.DiscountAmount.Amount, Currency = i.DiscountAmount.Currency },
                TaxAmount = new MoneyDto { Amount = i.TaxAmount.Amount, Currency = i.TaxAmount.Currency },
                TotalAmount = new MoneyDto { Amount = i.TotalAmount.Amount, Currency = i.TotalAmount.Currency },
                ImageUrl = i.ImageUrl,
                Weight = i.Weight
            }).ToList(),
            StatusHistory = order.StatusHistory
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new OrderStatusHistoryDto
                {
                    Status = h.Status,
                    Reason = h.Reason,
                    ChangedAt = h.ChangedAt,
                    ChangedBy = h.ChangedBy
                }).ToList(),
            Shipment = shipment
        };
    }
}
