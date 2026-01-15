using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Finance;

/// <summary>
/// Query to get financial details for a specific order.
/// </summary>
[RequirePermission("finance.view")]
[RequireFeature("finance_management")]
public record GetOrderFinancialsQuery : IRequest<Result<OrderFinancialsDto>>, ITenantRequest
{
    public Guid OrderId { get; init; }
}

public class GetOrderFinancialsQueryHandler : IRequestHandler<GetOrderFinancialsQuery, Result<OrderFinancialsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetOrderFinancialsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<OrderFinancialsDto>> Handle(
        GetOrderFinancialsQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Channel)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Result<OrderFinancialsDto>.Failure("Order not found");
        }

        // Get products for cost lookup
        var productIds = order.Items
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        // Get associated expenses
        var associatedExpenses = await _dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.ReferenceType == "Order" && e.ReferenceId == order.Id)
            .ToListAsync(cancellationToken);

        // Calculate item financials
        var itemFinancials = new List<OrderItemFinancialsDto>();
        decimal totalCostOfGoods = 0;

        foreach (var item in order.Items)
        {
            decimal unitCost = 0;
            if (item.ProductId.HasValue && products.TryGetValue(item.ProductId.Value, out var product))
            {
                unitCost = product.CostPrice.Amount;
            }

            var totalCost = unitCost * item.Quantity;
            var totalPrice = item.TotalAmount.Amount;
            var itemProfit = totalPrice - totalCost;
            var profitMargin = totalPrice > 0 ? (itemProfit / totalPrice) * 100 : 0;

            totalCostOfGoods += totalCost;

            itemFinancials.Add(new OrderItemFinancialsDto
            {
                ItemId = item.Id,
                Sku = item.Sku,
                Name = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice.Amount,
                UnitCost = unitCost,
                TotalPrice = totalPrice,
                TotalCost = totalCost,
                ItemProfit = itemProfit,
                ProfitMargin = Math.Round(profitMargin, 2)
            });
        }

        // Calculate expense breakdown
        var shippingCost = associatedExpenses
            .Where(e => e.Category == ExpenseCategory.Shipping)
            .Sum(e => e.Amount.Amount);

        var platformFee = associatedExpenses
            .Where(e => e.Category == ExpenseCategory.PlatformFees)
            .Sum(e => e.Amount.Amount);

        var paymentProcessingFee = associatedExpenses
            .Where(e => e.Category == ExpenseCategory.PaymentProcessing)
            .Sum(e => e.Amount.Amount);

        var packagingCost = associatedExpenses
            .Where(e => e.Category == ExpenseCategory.Packaging)
            .Sum(e => e.Amount.Amount);

        var totalCosts = totalCostOfGoods + shippingCost + platformFee + paymentProcessingFee + packagingCost;

        // Calculate profits
        var grossProfit = order.TotalAmount.Amount - totalCostOfGoods;
        var netProfit = order.TotalAmount.Amount - totalCosts;
        var profitMarginValue = order.TotalAmount.Amount > 0
            ? (netProfit / order.TotalAmount.Amount) * 100
            : 0;

        // Map associated expenses to DTOs
        var expenseDtos = associatedExpenses.Select(e => new ExpenseListDto
        {
            Id = e.Id,
            Category = e.Category,
            Amount = e.Amount.Amount,
            Currency = e.Amount.Currency,
            Description = e.Description,
            ExpenseDate = e.ExpenseDate,
            Vendor = e.Vendor,
            InvoiceNumber = e.InvoiceNumber,
            IsRecurring = e.IsRecurring,
            CreatedAt = e.CreatedAt
        }).ToList();

        var dto = new OrderFinancialsDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            ChannelName = order.Channel?.Name ?? "Unknown",
            Status = order.Status.ToString(),
            Subtotal = order.Subtotal.Amount,
            DiscountAmount = order.DiscountAmount.Amount,
            TaxAmount = order.TaxAmount.Amount,
            ShippingCharged = order.ShippingAmount.Amount,
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency,
            CostOfGoods = totalCostOfGoods,
            ShippingCost = shippingCost,
            PlatformFee = platformFee,
            PaymentProcessingFee = paymentProcessingFee,
            PackagingCost = packagingCost,
            TotalCosts = totalCosts,
            GrossProfit = grossProfit,
            NetProfit = netProfit,
            ProfitMargin = Math.Round(profitMarginValue, 2),
            Items = itemFinancials,
            AssociatedExpenses = expenseDtos
        };

        return Result<OrderFinancialsDto>.Success(dto);
    }
}
