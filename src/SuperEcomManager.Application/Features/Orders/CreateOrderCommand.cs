using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Command to create a manual order (phone order, walk-in, etc.).
/// </summary>
[RequirePermission("orders.create")]
[RequireFeature("order_management")]
public record CreateOrderCommand : IRequest<Result<OrderDetailDto>>, ITenantRequest
{
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
    public PaymentStatus PaymentStatus { get; init; } = PaymentStatus.Pending;

    // Financials
    public decimal ShippingAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public string Currency { get; init; } = "INR";

    // Notes
    public string? CustomerNotes { get; init; }
    public string? InternalNotes { get; init; }

    // Optional: assign to a specific channel (defaults to Manual/Custom channel)
    public Guid? ChannelId { get; init; }
}

/// <summary>
/// DTO for creating an address.
/// </summary>
public record CreateAddressDto
{
    public string Name { get; init; } = string.Empty;
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = "India";
    public string? Phone { get; init; }
}

/// <summary>
/// DTO for creating an order item.
/// </summary>
public record CreateOrderItemDto
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrderCreationServiceFactory _orderCreationServiceFactory;

    public CreateOrderCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        IOrderCreationServiceFactory orderCreationServiceFactory)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _orderCreationServiceFactory = orderCreationServiceFactory;
    }

    public async Task<Result<OrderDetailDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate items
        if (request.Items.Count == 0)
        {
            return Result<OrderDetailDto>.Failure("Order must have at least one item");
        }

        // Get or create the channel for manual orders
        Guid channelId;
        string channelName;
        ChannelType channelType;

        if (request.ChannelId.HasValue)
        {
            var channel = await _dbContext.SalesChannels
                .FirstOrDefaultAsync(c => c.Id == request.ChannelId.Value, cancellationToken);

            if (channel == null)
            {
                return Result<OrderDetailDto>.Failure("Specified channel not found");
            }

            channelId = channel.Id;
            channelName = channel.Name;
            channelType = channel.Type;
        }
        else
        {
            // Find or create a "Manual Orders" channel
            var manualChannel = await _dbContext.SalesChannels
                .FirstOrDefaultAsync(c => c.Type == ChannelType.Custom && c.Name == "Manual Orders", cancellationToken);

            if (manualChannel == null)
            {
                // Create the manual orders channel
                manualChannel = Domain.Entities.Channels.SalesChannel.Create(
                    "Manual Orders",
                    ChannelType.Custom);
                manualChannel.MarkConnected(); // Mark as connected
                await _dbContext.SalesChannels.AddAsync(manualChannel, cancellationToken);
            }

            channelId = manualChannel.Id;
            channelName = manualChannel.Name;
            channelType = manualChannel.Type;
        }

        // Create shipping address
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

        // Calculate subtotal from items
        var subtotal = request.Items.Sum(i => i.UnitPrice * i.Quantity - i.DiscountAmount);
        var totalAmount = subtotal - request.DiscountAmount + request.TaxAmount + request.ShippingAmount;

        // Create the order
        var externalOrderId = $"MANUAL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var order = Order.Create(
            channelId: channelId,
            externalOrderId: externalOrderId,
            customerName: request.CustomerName,
            shippingAddress: shippingAddress,
            totalAmount: new Money(totalAmount, request.Currency),
            orderDate: DateTime.UtcNow,
            externalOrderNumber: null
        );

        // Set customer info
        order.SetCustomerInfo(request.CustomerEmail, request.CustomerPhone);

        // Set billing address if provided
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

        // Set payment info
        order.SetPaymentInfo(request.PaymentMethod, request.PaymentStatus);

        // Set financials
        order.SetFinancials(
            new Money(subtotal, request.Currency),
            new Money(request.DiscountAmount, request.Currency),
            new Money(request.TaxAmount, request.Currency),
            new Money(request.ShippingAmount, request.Currency)
        );

        // Set notes
        order.SetNotes(request.CustomerNotes, request.InternalNotes);

        // Add order items
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

            order.AddItem(item);
        }

        // If payment is already done (prepaid), confirm the order
        if (request.PaymentStatus == PaymentStatus.Paid)
        {
            order.UpdateStatus(OrderStatus.Confirmed, _currentUserService.UserId, "Manual order created with payment");
        }

        await _dbContext.Orders.AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // If the channel supports external order creation, push to the platform
        if (_orderCreationServiceFactory.IsSupported(channelType))
        {
            var orderCreationService = _orderCreationServiceFactory.GetService(channelType);
            if (orderCreationService != null)
            {
                var creationResult = await orderCreationService.CreateOrderAsync(channelId, order, cancellationToken);

                if (creationResult.Success && !string.IsNullOrEmpty(creationResult.ExternalOrderId))
                {
                    // Update the order with the platform's external order ID
                    order.SetExternalOrderInfo(creationResult.ExternalOrderId, creationResult.ExternalOrderNumber);

                    // Store platform-specific data if available
                    if (creationResult.PlatformData != null)
                    {
                        order.SetPlatformData(System.Text.Json.JsonSerializer.Serialize(creationResult.PlatformData));
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                else if (!creationResult.Success)
                {
                    // Log the failure but don't fail the order creation
                    // The order is created locally; external sync can be retried later
                    order.SetNotes(
                        order.CustomerNotes,
                        $"{order.InternalNotes}\n[External sync failed: {creationResult.ErrorMessage}]".Trim()
                    );
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        // Return the order details
        var result = new OrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            ExternalOrderId = order.ExternalOrderId,
            ChannelId = channelId,
            ChannelName = channelName,
            ChannelType = channelType,
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

        return Result<OrderDetailDto>.Success(result);
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
