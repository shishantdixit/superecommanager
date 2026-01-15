using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Command to bulk update status of multiple orders.
/// </summary>
[RequirePermission("orders.edit")]
[RequireFeature("order_management")]
public record BulkUpdateOrdersCommand : IRequest<Result<BulkUpdateResult>>, ITenantRequest
{
    public List<Guid> OrderIds { get; init; } = new();
    public OrderStatus NewStatus { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Result of bulk update operation.
/// </summary>
public record BulkUpdateResult
{
    public int TotalRequested { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public List<BulkUpdateError> Errors { get; init; } = new();
}

/// <summary>
/// Error detail for failed bulk update.
/// </summary>
public record BulkUpdateError
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

public class BulkUpdateOrdersCommandHandler : IRequestHandler<BulkUpdateOrdersCommand, Result<BulkUpdateResult>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BulkUpdateOrdersCommandHandler> _logger;

    public BulkUpdateOrdersCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<BulkUpdateOrdersCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<BulkUpdateResult>> Handle(
        BulkUpdateOrdersCommand request,
        CancellationToken cancellationToken)
    {
        if (request.OrderIds.Count == 0)
        {
            return Result<BulkUpdateResult>.Failure("No orders specified");
        }

        if (request.OrderIds.Count > 100)
        {
            return Result<BulkUpdateResult>.Failure("Cannot update more than 100 orders at once");
        }

        var orders = await _dbContext.Orders
            .Where(o => request.OrderIds.Contains(o.Id) && o.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var errors = new List<BulkUpdateError>();
        var successCount = 0;

        foreach (var order in orders)
        {
            try
            {
                order.UpdateStatus(request.NewStatus, _currentUserService.UserId, request.Reason);
                successCount++;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new BulkUpdateError
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Add errors for orders not found
        var foundIds = orders.Select(o => o.Id).ToHashSet();
        var notFoundIds = request.OrderIds.Where(id => !foundIds.Contains(id));
        foreach (var id in notFoundIds)
        {
            errors.Add(new BulkUpdateError
            {
                OrderId = id,
                OrderNumber = "Unknown",
                ErrorMessage = "Order not found"
            });
        }

        if (successCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk updated {SuccessCount} orders to status {NewStatus} by {UserId}",
                successCount, request.NewStatus, _currentUserService.UserId);
        }

        var result = new BulkUpdateResult
        {
            TotalRequested = request.OrderIds.Count,
            SuccessCount = successCount,
            FailedCount = errors.Count,
            Errors = errors
        };

        return Result<BulkUpdateResult>.Success(result);
    }
}
