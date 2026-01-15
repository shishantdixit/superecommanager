using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Command to cancel an order.
/// </summary>
[RequirePermission("orders.cancel")]
[RequireFeature("order_management")]
public record CancelOrderCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid OrderId { get; init; }
    public string? Reason { get; init; }
}

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.DeletedAt == null, cancellationToken);

        if (order == null)
        {
            return Result<bool>.Failure("Order not found");
        }

        try
        {
            order.Cancel(_currentUserService.UserId, request.Reason);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} cancelled by {UserId}. Reason: {Reason}",
                order.Id, _currentUserService.UserId, request.Reason);

            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "Cannot cancel order {OrderId}: {Message}",
                order.Id, ex.Message);
            return Result<bool>.Failure(ex.Message);
        }
    }
}
