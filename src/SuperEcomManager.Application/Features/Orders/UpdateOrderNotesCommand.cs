using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Orders;

/// <summary>
/// Command to update internal notes for an order.
/// </summary>
[RequirePermission("orders.edit")]
[RequireFeature("order_management")]
public record UpdateOrderNotesCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid OrderId { get; init; }
    public string? InternalNotes { get; init; }
}

public class UpdateOrderNotesCommandHandler : IRequestHandler<UpdateOrderNotesCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<UpdateOrderNotesCommandHandler> _logger;

    public UpdateOrderNotesCommandHandler(
        ITenantDbContext dbContext,
        ILogger<UpdateOrderNotesCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        UpdateOrderNotesCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.DeletedAt == null, cancellationToken);

        if (order == null)
        {
            return Result<bool>.Failure("Order not found");
        }

        order.SetNotes(order.CustomerNotes, request.InternalNotes);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated notes for order {OrderId}", order.Id);

        return Result<bool>.Success(true);
    }
}
