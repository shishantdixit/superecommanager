using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Query to get a single order by ID with full details.
/// </summary>
[RequirePermission("orders.view")]
[RequireFeature("order_management")]
public record GetOrderByIdQuery : IRequest<Result<OrderDetailDto>>, ITenantRequest
{
    public Guid OrderId { get; init; }
}

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetOrderByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<OrderDetailDto>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Channel)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.DeletedAt == null, cancellationToken);

        if (order == null)
        {
            return Result<OrderDetailDto>.Failure("Order not found");
        }

        // Get shipment if exists
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .Where(s => s.OrderId == order.Id && s.DeletedAt == null)
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

        var dto = new OrderDetailDto
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

        return Result<OrderDetailDto>.Success(dto);
    }
}
