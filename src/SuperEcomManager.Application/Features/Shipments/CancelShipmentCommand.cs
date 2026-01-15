using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Command to cancel a shipment.
/// </summary>
[RequirePermission("shipments.cancel")]
[RequireFeature("shipping_management")]
public record CancelShipmentCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid ShipmentId { get; init; }
    public string? Reason { get; init; }
}

public class CancelShipmentCommandHandler : IRequestHandler<CancelShipmentCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<CancelShipmentCommandHandler> _logger;

    public CancelShipmentCommandHandler(
        ITenantDbContext dbContext,
        ILogger<CancelShipmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        CancelShipmentCommand request,
        CancellationToken cancellationToken)
    {
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId && s.DeletedAt == null, cancellationToken);

        if (shipment == null)
        {
            return Result<bool>.Failure("Shipment not found");
        }

        // Check if shipment can be cancelled
        if (!CanBeCancelled(shipment.Status))
        {
            return Result<bool>.Failure($"Cannot cancel shipment in {shipment.Status} status");
        }

        shipment.UpdateStatus(ShipmentStatus.Cancelled, null, request.Reason ?? "Cancelled by user");
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Shipment {ShipmentId} cancelled. Reason: {Reason}",
            shipment.Id, request.Reason);

        return Result<bool>.Success(true);
    }

    private static bool CanBeCancelled(ShipmentStatus status)
    {
        // Can only cancel before pickup
        return status is ShipmentStatus.Created or ShipmentStatus.Manifested;
    }
}
