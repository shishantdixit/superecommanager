using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Command to update an existing order.
/// Only allowed for orders that have not been shipped/delivered/cancelled.
/// </summary>
[RequirePermission("orders.edit")]
[RequireFeature("order_management")]
public record UpdateOrderCommand : IRequest<Result<OrderDetailDto>>, ITenantRequest
{
    public Guid OrderId { get; init; }

    // Customer info
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }

    // Shipping address
    public CreateAddressDto ShippingAddress { get; init; } = null!;
    public CreateAddressDto? BillingAddress { get; init; }

    // Order items
    public List<CreateOrderItemDto> Items { get; init; } = new();

    // Payment
    public PaymentMethod PaymentMethod { get; init; }
    public PaymentStatus PaymentStatus { get; init; }

    // Financials
    public decimal ShippingAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public string Currency { get; init; } = "INR";

    // Notes
    public string? CustomerNotes { get; init; }
    public string? InternalNotes { get; init; }

    /// <summary>
    /// Whether to sync changes to the external channel (e.g., Shopify).
    /// If true and the order originated from a channel, changes will be pushed to that channel.
    /// </summary>
    public bool SyncToChannel { get; init; } = false;
}

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, Result<OrderDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrderUpdateServiceFactory _orderUpdateServiceFactory;
    private readonly ILogger<UpdateOrderCommandHandler> _logger;

    public UpdateOrderCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        IOrderUpdateServiceFactory orderUpdateServiceFactory,
        ILogger<UpdateOrderCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _orderUpdateServiceFactory = orderUpdateServiceFactory;
        _logger = logger;
    }

    public async Task<Result<OrderDetailDto>> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate items first (before transaction)
        if (request.Items.Count == 0)
        {
            return Result<OrderDetailDto>.Failure("Order must have at least one item");
        }

        // Use execution strategy to support transactions with retry policy
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Use a transaction to ensure all operations succeed or all fail together
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Find the order with channel (don't include items - we'll delete and recreate them)
                var order = await _dbContext.Orders
                    .Include(o => o.Channel)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

                if (order == null)
                {
                    return Result<OrderDetailDto>.Failure("Order not found");
                }

                // Check if order can be edited
                if (!order.CanBeEdited())
                {
                    return Result<OrderDetailDto>.Failure($"Order in status {order.Status} cannot be edited");
                }

            // Update customer info
            order.UpdateCustomerName(request.CustomerName);
            order.SetCustomerInfo(request.CustomerEmail, request.CustomerPhone);

            // Update shipping address
            var shippingAddress = new Address(
                name: request.ShippingAddress.Name,
                phone: request.ShippingAddress.Phone,
                line1: request.ShippingAddress.Line1,
                line2: request.ShippingAddress.Line2,
                city: request.ShippingAddress.City,
                state: request.ShippingAddress.State,
                postalCode: request.ShippingAddress.PostalCode,
                country: request.ShippingAddress.Country
            );
            order.UpdateShippingAddress(shippingAddress);

            // Update billing address
            if (request.BillingAddress != null)
            {
                var billingAddress = new Address(
                    name: request.BillingAddress.Name,
                    phone: request.BillingAddress.Phone,
                    line1: request.BillingAddress.Line1,
                    line2: request.BillingAddress.Line2,
                    city: request.BillingAddress.City,
                    state: request.BillingAddress.State,
                    postalCode: request.BillingAddress.PostalCode,
                    country: request.BillingAddress.Country
                );
                order.SetBillingAddress(billingAddress);
            }
            else
            {
                order.SetBillingAddress(null);
            }

            // Update payment info
            order.SetPaymentInfo(request.PaymentMethod, request.PaymentStatus);

            // Update notes
            order.SetNotes(request.CustomerNotes, request.InternalNotes);

            // Calculate subtotal from new items
            var subtotal = request.Items.Sum(i => i.UnitPrice * i.Quantity - i.DiscountAmount);
            var totalAmount = subtotal - request.DiscountAmount + request.TaxAmount + request.ShippingAmount;

            // Set financials
            order.SetFinancials(
                new Money(subtotal, request.Currency),
                new Money(request.DiscountAmount, request.Currency),
                new Money(request.TaxAmount, request.Currency),
                new Money(request.ShippingAmount, request.Currency)
            );

            // Save order changes first
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Delete existing items directly from database
            await _dbContext.OrderItems
                .Where(i => i.OrderId == order.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // Clear the in-memory collection
            order.ClearItems();

            // Create and add new items directly to DbContext
            var newItems = new List<OrderItem>();
            foreach (var itemDto in request.Items)
            {
                var item = new OrderItem(
                    orderId: order.Id,
                    sku: itemDto.Sku,
                    name: itemDto.Name,
                    quantity: itemDto.Quantity,
                    unitPrice: new Money(itemDto.UnitPrice, request.Currency),
                    externalProductId: null,
                    variantName: itemDto.VariantName
                );

                if (itemDto.DiscountAmount > 0 || itemDto.TaxAmount > 0)
                {
                    item.SetFinancials(
                        new Money(itemDto.DiscountAmount, request.Currency),
                        new Money(itemDto.TaxAmount, request.Currency)
                    );
                }

                newItems.Add(item);
                order.AddItem(item);
            }

            // Add new items to DbContext and save
            _dbContext.OrderItems.AddRange(newItems);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Sync to external channel if requested
            string? syncError = null;
            if (request.SyncToChannel && order.Channel != null && !string.IsNullOrEmpty(order.ExternalOrderId))
            {
                var updateService = _orderUpdateServiceFactory.GetService(order.Channel.Type);
                if (updateService != null)
                {
                    _logger.LogInformation(
                        "Syncing order {OrderNumber} update to {ChannelType}",
                        order.OrderNumber,
                        order.Channel.Type);

                    var syncResult = await updateService.UpdateOrderAsync(
                        order.ChannelId,
                        order.ExternalOrderId,
                        order,
                        cancellationToken);

                    if (!syncResult.Success)
                    {
                        // Log the error but don't fail the operation - local update succeeded
                        _logger.LogWarning(
                            "Failed to sync order {OrderNumber} to {ChannelType}: {Error}",
                            order.OrderNumber,
                            order.Channel.Type,
                            syncResult.ErrorMessage);
                        syncError = syncResult.ErrorMessage;
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Successfully synced order {OrderNumber} to {ChannelType}",
                            order.OrderNumber,
                            order.Channel.Type);
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "No update service available for channel type {ChannelType}",
                        order.Channel.Type);
                }
            }

            // Return the updated order details
            var result = new OrderDetailDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                ExternalOrderId = order.ExternalOrderId,
                ChannelId = order.ChannelId,
                ChannelName = order.Channel?.Name ?? "Unknown",
                ChannelType = order.Channel?.Type ?? ChannelType.Custom,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                FulfillmentStatus = order.FulfillmentStatus,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                CustomerPhone = order.CustomerPhone,
                ShippingAddress = MapAddress(order.ShippingAddress),
                BillingAddress = order.BillingAddress != null ? MapAddress(order.BillingAddress) : null,
                Subtotal = order.Subtotal.Amount,
                DiscountAmount = order.DiscountAmount.Amount,
                TaxAmount = order.TaxAmount.Amount,
                ShippingAmount = order.ShippingAmount.Amount,
                TotalAmount = order.TotalAmount.Amount,
                Currency = order.TotalAmount.Currency,
                PaymentMethod = order.PaymentMethod,
                IsCOD = order.IsCOD,
                OrderDate = order.OrderDate,
                CreatedAt = order.CreatedAt,
                CustomerNotes = order.CustomerNotes,
                InternalNotes = order.InternalNotes,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    Sku = i.Sku,
                    Name = i.Name,
                    VariantName = i.VariantName,
                    Quantity = i.Quantity,
                    UnitPrice = new MoneyDto { Amount = i.UnitPrice.Amount, Currency = i.UnitPrice.Currency },
                    TotalAmount = new MoneyDto { Amount = i.TotalAmount.Amount, Currency = i.TotalAmount.Currency },
                    DiscountAmount = new MoneyDto { Amount = i.DiscountAmount.Amount, Currency = i.DiscountAmount.Currency },
                    TaxAmount = new MoneyDto { Amount = i.TaxAmount.Amount, Currency = i.TaxAmount.Currency }
                }).ToList()
            };

            await transaction.CommitAsync(cancellationToken);
            return Result<OrderDetailDto>.Success(result);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update order {OrderId}", request.OrderId);
            return Result<OrderDetailDto>.Failure($"Failed to update order: {ex.Message}");
        }
        });
    }

    private static AddressDto MapAddress(Address address) => new()
    {
        Name = address.Name,
        Line1 = address.Line1,
        Line2 = address.Line2,
        City = address.City,
        State = address.State,
        PostalCode = address.PostalCode,
        Country = address.Country,
        Phone = address.Phone
    };
}
