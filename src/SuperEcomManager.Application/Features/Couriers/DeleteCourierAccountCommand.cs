using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Command to delete a courier account.
/// </summary>
[RequirePermission("couriers.delete")]
[RequireFeature("courier_management")]
public record DeleteCourierAccountCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid AccountId { get; init; }
}

public class DeleteCourierAccountCommandHandler : IRequestHandler<DeleteCourierAccountCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<DeleteCourierAccountCommandHandler> _logger;

    public DeleteCourierAccountCommandHandler(
        ITenantDbContext dbContext,
        ILogger<DeleteCourierAccountCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        DeleteCourierAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.AccountId, cancellationToken);

        if (account == null)
        {
            return Result<bool>.Failure("Courier account not found");
        }

        // Check if there are any active shipments using this courier
        var hasActiveShipments = await _dbContext.Shipments
            .AnyAsync(s => s.CourierType == account.CourierType &&
                          s.Status != Domain.Enums.ShipmentStatus.Delivered &&
                          s.Status != Domain.Enums.ShipmentStatus.Cancelled &&
                          s.Status != Domain.Enums.ShipmentStatus.RTODelivered,
                cancellationToken);

        if (hasActiveShipments)
        {
            return Result<bool>.Failure("Cannot delete courier account with active shipments");
        }

        // Soft delete
        account.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted courier account {AccountId}", account.Id);

        return Result<bool>.Success(true);
    }
}
