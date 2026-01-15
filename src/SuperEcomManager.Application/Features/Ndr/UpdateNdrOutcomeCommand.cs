using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.NDR;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Ndr;

/// <summary>
/// Command to update the outcome/resolution of an NDR case.
/// </summary>
[RequirePermission("ndr.resolve")]
[RequireFeature("ndr_management")]
public record UpdateNdrOutcomeCommand : IRequest<Result<NdrDetailDto>>, ITenantRequest
{
    public Guid NdrRecordId { get; init; }
    public NdrStatus NewStatus { get; init; }
    public string? Resolution { get; init; }
}

public class UpdateNdrOutcomeCommandHandler : IRequestHandler<UpdateNdrOutcomeCommand, Result<NdrDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateNdrOutcomeCommandHandler> _logger;

    public UpdateNdrOutcomeCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<UpdateNdrOutcomeCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<NdrDetailDto>> Handle(
        UpdateNdrOutcomeCommand request,
        CancellationToken cancellationToken)
    {
        var ndr = await _dbContext.NdrRecords
            .Include(n => n.Actions)
            .Include(n => n.Remarks)
            .FirstOrDefaultAsync(n => n.Id == request.NdrRecordId, cancellationToken);

        if (ndr == null)
        {
            return Result<NdrDetailDto>.Failure("NDR case not found");
        }

        // Check if case is already resolved
        if (ndr.Status == NdrStatus.ClosedDelivered ||
            ndr.Status == NdrStatus.ClosedRTO ||
            ndr.Status == NdrStatus.ClosedAddressUpdated)
        {
            return Result<NdrDetailDto>.Failure("NDR case is already closed");
        }

        // Validate the new status
        if (!IsValidStatusTransition(ndr.Status, request.NewStatus))
        {
            return Result<NdrDetailDto>.Failure(
                $"Invalid status transition from {ndr.Status} to {request.NewStatus}");
        }

        var oldStatus = ndr.Status;

        // Update status
        ndr.Resolve(request.NewStatus, request.Resolution);

        // Determine action type based on status
        var actionType = request.NewStatus switch
        {
            NdrStatus.RTOInitiated => NdrActionType.RTOInitiated,
            NdrStatus.Escalated => NdrActionType.Escalated,
            NdrStatus.ClosedDelivered => NdrActionType.RemarkAdded,
            NdrStatus.ClosedRTO => NdrActionType.RemarkAdded,
            NdrStatus.ClosedAddressUpdated => NdrActionType.AddressUpdated,
            _ => NdrActionType.RemarkAdded
        };

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        // Add action
        var action = NdrAction.Create(
            ndr.Id,
            actionType,
            currentUserId,
            $"Status changed from {oldStatus} to {request.NewStatus}",
            request.Resolution);
        ndr.AddAction(action);

        // Update shipment status if needed
        if (request.NewStatus == NdrStatus.RTOInitiated)
        {
            var shipment = await _dbContext.Shipments
                .FirstOrDefaultAsync(s => s.Id == ndr.ShipmentId && s.DeletedAt == null, cancellationToken);

            if (shipment != null && shipment.Status != ShipmentStatus.RTOInitiated)
            {
                shipment.UpdateStatus(ShipmentStatus.RTOInitiated, null, "NDR case initiated RTO");
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "NDR case {NdrRecordId} outcome updated from {OldStatus} to {NewStatus} by user {UserId}",
            ndr.Id, oldStatus, request.NewStatus, _currentUserService.UserId);

        // Get related data for response
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ndr.OrderId, cancellationToken);

        var shipmentData = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ndr.ShipmentId, cancellationToken);

        // Get user names
        var userIds = ndr.Actions.Select(a => a.PerformedByUserId)
            .Union(ndr.Remarks.Select(r => r.UserId))
            .Distinct()
            .ToList();

        if (ndr.AssignedToUserId.HasValue)
            userIds.Add(ndr.AssignedToUserId.Value);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var dto = MapToDetailDto(ndr, order, shipmentData, users);
        return Result<NdrDetailDto>.Success(dto);
    }

    private static bool IsValidStatusTransition(NdrStatus current, NdrStatus next)
    {
        // Define valid status transitions
        return (current, next) switch
        {
            // From Open
            (NdrStatus.Open, NdrStatus.Assigned) => true,
            (NdrStatus.Open, NdrStatus.CustomerContacted) => true,
            (NdrStatus.Open, NdrStatus.ReattemptScheduled) => true,
            (NdrStatus.Open, NdrStatus.RTOInitiated) => true,
            (NdrStatus.Open, NdrStatus.Escalated) => true,

            // From Assigned
            (NdrStatus.Assigned, NdrStatus.CustomerContacted) => true,
            (NdrStatus.Assigned, NdrStatus.ReattemptScheduled) => true,
            (NdrStatus.Assigned, NdrStatus.RTOInitiated) => true,
            (NdrStatus.Assigned, NdrStatus.Escalated) => true,

            // From CustomerContacted
            (NdrStatus.CustomerContacted, NdrStatus.ReattemptScheduled) => true,
            (NdrStatus.CustomerContacted, NdrStatus.RTOInitiated) => true,
            (NdrStatus.CustomerContacted, NdrStatus.Escalated) => true,
            (NdrStatus.CustomerContacted, NdrStatus.ClosedAddressUpdated) => true,

            // From ReattemptScheduled
            (NdrStatus.ReattemptScheduled, NdrStatus.ReattemptInProgress) => true,
            (NdrStatus.ReattemptScheduled, NdrStatus.RTOInitiated) => true,

            // From ReattemptInProgress
            (NdrStatus.ReattemptInProgress, NdrStatus.Delivered) => true,
            (NdrStatus.ReattemptInProgress, NdrStatus.RTOInitiated) => true,
            (NdrStatus.ReattemptInProgress, NdrStatus.ReattemptScheduled) => true, // Failed, reschedule

            // From Delivered
            (NdrStatus.Delivered, NdrStatus.ClosedDelivered) => true,

            // From RTOInitiated
            (NdrStatus.RTOInitiated, NdrStatus.ClosedRTO) => true,

            // From Escalated
            (NdrStatus.Escalated, NdrStatus.ReattemptScheduled) => true,
            (NdrStatus.Escalated, NdrStatus.RTOInitiated) => true,
            (NdrStatus.Escalated, NdrStatus.ClosedDelivered) => true,
            (NdrStatus.Escalated, NdrStatus.ClosedRTO) => true,

            _ => false
        };
    }

    private static NdrDetailDto MapToDetailDto(
        NdrRecord ndr,
        Domain.Entities.Orders.Order? order,
        Domain.Entities.Shipments.Shipment? shipment,
        Dictionary<Guid, string> users)
    {
        return new NdrDetailDto
        {
            Id = ndr.Id,
            ShipmentId = ndr.ShipmentId,
            OrderId = ndr.OrderId,
            OrderNumber = order?.OrderNumber ?? "Unknown",
            ShipmentNumber = shipment?.ShipmentNumber ?? "Unknown",
            AwbNumber = ndr.AwbNumber,
            Status = ndr.Status,
            ReasonCode = ndr.ReasonCode,
            ReasonDescription = ndr.ReasonDescription,
            NdrDate = ndr.NdrDate,
            AssignedToUserId = ndr.AssignedToUserId,
            AssignedToUserName = ndr.AssignedToUserId.HasValue
                ? users.GetValueOrDefault(ndr.AssignedToUserId.Value, "Unknown")
                : null,
            AssignedAt = ndr.AssignedAt,
            AttemptCount = ndr.AttemptCount,
            NextFollowUpAt = ndr.NextFollowUpAt,
            ResolvedAt = ndr.ResolvedAt,
            Resolution = ndr.Resolution,
            CreatedAt = ndr.CreatedAt,
            UpdatedAt = ndr.UpdatedAt,
            CustomerName = order?.CustomerName ?? "Unknown",
            CustomerPhone = order?.CustomerPhone,
            CustomerEmail = order?.CustomerEmail,
            DeliveryAddress = shipment != null ? new AddressDto
            {
                Name = shipment.DeliveryAddress.Name,
                Line1 = shipment.DeliveryAddress.Line1,
                Line2 = shipment.DeliveryAddress.Line2,
                City = shipment.DeliveryAddress.City,
                State = shipment.DeliveryAddress.State,
                PostalCode = shipment.DeliveryAddress.PostalCode,
                Country = shipment.DeliveryAddress.Country,
                Phone = shipment.DeliveryAddress.Phone
            } : new AddressDto(),
            IsCOD = shipment?.IsCOD ?? false,
            CODAmount = shipment?.CODAmount?.Amount,
            Actions = ndr.Actions
                .OrderByDescending(a => a.PerformedAt)
                .Select(a => new NdrActionDto
                {
                    Id = a.Id,
                    ActionType = a.ActionType,
                    PerformedByUserId = a.PerformedByUserId,
                    PerformedByUserName = users.GetValueOrDefault(a.PerformedByUserId, "Unknown"),
                    PerformedAt = a.PerformedAt,
                    Details = a.Details,
                    Outcome = a.Outcome,
                    CallDurationSeconds = a.CallDurationSeconds
                })
                .ToList(),
            Remarks = ndr.Remarks
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new NdrRemarkDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = users.GetValueOrDefault(r.UserId, "Unknown"),
                    Content = r.Content,
                    CreatedAt = r.CreatedAt,
                    IsInternal = r.IsInternal
                })
                .ToList()
        };
    }
}
