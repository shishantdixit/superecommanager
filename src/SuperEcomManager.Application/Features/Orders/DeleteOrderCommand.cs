using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Command to soft-delete an order.
/// Only allowed for orders in Pending or Cancelled status.
/// </summary>
[RequirePermission("orders.delete")]
[RequireFeature("order_management")]
public record DeleteOrderCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid OrderId { get; init; }
}

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteOrderCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Result<bool>.Failure("Order not found");
        }

        if (!order.CanBeDeleted())
        {
            return Result<bool>.Failure($"Order in status {order.Status} cannot be deleted. Only Pending or Cancelled orders can be deleted.");
        }

        order.SoftDelete(_currentUserService.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
