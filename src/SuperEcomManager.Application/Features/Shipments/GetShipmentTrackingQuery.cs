using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Shipments;

/// <summary>
/// Query to get tracking info for a shipment from local database.
/// </summary>
[RequirePermission("shipments.view")]
[RequireFeature("shipping_management")]
public record GetShipmentTrackingQuery : IRequest<Result<TrackingInfoDto>>, ITenantRequest
{
    public Guid ShipmentId { get; init; }
}

public class GetShipmentTrackingQueryHandler : IRequestHandler<GetShipmentTrackingQuery, Result<TrackingInfoDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetShipmentTrackingQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<TrackingInfoDto>> Handle(
        GetShipmentTrackingQuery request,
        CancellationToken cancellationToken)
    {
        var shipment = await _dbContext.Shipments
            .Include(s => s.TrackingEvents)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId && s.DeletedAt == null, cancellationToken);

        if (shipment == null)
        {
            return Result<TrackingInfoDto>.Failure("Shipment not found");
        }

        // Return local tracking events
        var dto = new TrackingInfoDto
        {
            AwbNumber = shipment.AwbNumber ?? string.Empty,
            CourierName = shipment.CourierName,
            CurrentStatus = shipment.Status,
            CurrentLocation = shipment.TrackingEvents
                .OrderByDescending(t => t.EventTime)
                .FirstOrDefault()?.Location,
            ExpectedDeliveryDate = shipment.ExpectedDeliveryDate,
            Events = shipment.TrackingEvents
                .OrderByDescending(t => t.EventTime)
                .Select(t => new TrackingEventDto
                {
                    EventTime = t.EventTime,
                    Status = t.Status.ToString(),
                    Location = t.Location,
                    Remarks = t.Remarks
                }).ToList()
        };

        return Result<TrackingInfoDto>.Success(dto);
    }
}
