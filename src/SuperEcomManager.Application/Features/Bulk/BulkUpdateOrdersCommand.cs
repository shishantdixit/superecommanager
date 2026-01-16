using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Bulk;

/// <summary>
/// Command to update multiple orders at once.
/// </summary>
[RequirePermission("orders.bulk")]
[RequireFeature("orders")]
public record BulkUpdateOrdersCommand : IRequest<Result<BulkOperationResultDto>>, ITenantRequest
{
    public List<Guid> OrderIds { get; init; } = new();
    public OrderStatus? NewStatus { get; init; }
    public string? InternalNotes { get; init; }
}

public class BulkUpdateOrdersCommandHandler : IRequestHandler<BulkUpdateOrdersCommand, Result<BulkOperationResultDto>>
{
    private readonly ITenantDbContext _dbContext;
    private const int MaxBatchSize = 100;

    public BulkUpdateOrdersCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BulkOperationResultDto>> Handle(
        BulkUpdateOrdersCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!request.OrderIds.Any())
        {
            return Result<BulkOperationResultDto>.Failure("No order IDs provided.");
        }

        if (request.OrderIds.Count > MaxBatchSize)
        {
            return Result<BulkOperationResultDto>.Failure($"Maximum {MaxBatchSize} orders can be updated at once.");
        }

        if (!request.NewStatus.HasValue && string.IsNullOrEmpty(request.InternalNotes))
        {
            return Result<BulkOperationResultDto>.Failure("No update parameters provided.");
        }

        var errors = new List<BulkOperationErrorDto>();
        var successfulIds = new List<Guid>();

        // Get all orders
        var orders = await _dbContext.Orders
            .Where(o => request.OrderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);

        var foundIds = orders.Select(o => o.Id).ToHashSet();
        var notFoundIds = request.OrderIds.Except(foundIds).ToList();

        // Add errors for not found orders
        foreach (var notFoundId in notFoundIds)
        {
            errors.Add(new BulkOperationErrorDto
            {
                ItemId = notFoundId,
                Error = "Order not found"
            });
        }

        // Update found orders
        foreach (var order in orders)
        {
            try
            {
                if (request.NewStatus.HasValue)
                {
                    // Validate status transition
                    if (!IsValidStatusTransition(order.Status, request.NewStatus.Value))
                    {
                        errors.Add(new BulkOperationErrorDto
                        {
                            ItemId = order.Id,
                            Reference = order.OrderNumber,
                            Error = $"Invalid status transition from {order.Status} to {request.NewStatus.Value}"
                        });
                        continue;
                    }

                    order.UpdateStatus(request.NewStatus.Value);
                }

                if (!string.IsNullOrEmpty(request.InternalNotes))
                {
                    order.SetNotes(null, request.InternalNotes);
                }

                successfulIds.Add(order.Id);
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationErrorDto
                {
                    ItemId = order.Id,
                    Reference = order.OrderNumber,
                    Error = ex.Message
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        stopwatch.Stop();

        var result = new BulkOperationResultDto
        {
            TotalRequested = request.OrderIds.Count,
            SuccessCount = successfulIds.Count,
            FailedCount = errors.Count,
            Errors = errors,
            SuccessfulIds = successfulIds,
            Duration = stopwatch.Elapsed
        };

        return Result<BulkOperationResultDto>.Success(result);
    }

    private static bool IsValidStatusTransition(OrderStatus current, OrderStatus newStatus)
    {
        // Define valid transitions
        return (current, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Processing) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Processing, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };
    }
}

/// <summary>
/// Command to cancel multiple orders at once.
/// </summary>
[RequirePermission("orders.bulk")]
[RequireFeature("orders")]
public record BulkCancelOrdersCommand : IRequest<Result<BulkOperationResultDto>>, ITenantRequest
{
    public List<Guid> OrderIds { get; init; } = new();
    public string? CancellationReason { get; init; }
}

public class BulkCancelOrdersCommandHandler : IRequestHandler<BulkCancelOrdersCommand, Result<BulkOperationResultDto>>
{
    private readonly ITenantDbContext _dbContext;
    private const int MaxBatchSize = 100;

    public BulkCancelOrdersCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BulkOperationResultDto>> Handle(
        BulkCancelOrdersCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!request.OrderIds.Any())
        {
            return Result<BulkOperationResultDto>.Failure("No order IDs provided.");
        }

        if (request.OrderIds.Count > MaxBatchSize)
        {
            return Result<BulkOperationResultDto>.Failure($"Maximum {MaxBatchSize} orders can be cancelled at once.");
        }

        var errors = new List<BulkOperationErrorDto>();
        var successfulIds = new List<Guid>();

        var orders = await _dbContext.Orders
            .Where(o => request.OrderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);

        var foundIds = orders.Select(o => o.Id).ToHashSet();
        var notFoundIds = request.OrderIds.Except(foundIds).ToList();

        foreach (var notFoundId in notFoundIds)
        {
            errors.Add(new BulkOperationErrorDto
            {
                ItemId = notFoundId,
                Error = "Order not found"
            });
        }

        foreach (var order in orders)
        {
            try
            {
                // Only cancel if not already cancelled or delivered
                if (order.Status == OrderStatus.Cancelled)
                {
                    errors.Add(new BulkOperationErrorDto
                    {
                        ItemId = order.Id,
                        Reference = order.OrderNumber,
                        Error = "Order is already cancelled"
                    });
                    continue;
                }

                if (order.Status == OrderStatus.Delivered)
                {
                    errors.Add(new BulkOperationErrorDto
                    {
                        ItemId = order.Id,
                        Reference = order.OrderNumber,
                        Error = "Cannot cancel delivered order"
                    });
                    continue;
                }

                order.Cancel(null, request.CancellationReason);
                successfulIds.Add(order.Id);
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationErrorDto
                {
                    ItemId = order.Id,
                    Reference = order.OrderNumber,
                    Error = ex.Message
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        stopwatch.Stop();

        var result = new BulkOperationResultDto
        {
            TotalRequested = request.OrderIds.Count,
            SuccessCount = successfulIds.Count,
            FailedCount = errors.Count,
            Errors = errors,
            SuccessfulIds = successfulIds,
            Duration = stopwatch.Elapsed
        };

        return Result<BulkOperationResultDto>.Success(result);
    }
}
