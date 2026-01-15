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
/// Command to assign an NDR case to a user.
/// </summary>
[RequirePermission("ndr.assign")]
[RequireFeature("ndr_management")]
public record AssignNdrCaseCommand : IRequest<Result<NdrDetailDto>>, ITenantRequest
{
    public Guid NdrRecordId { get; init; }
    public Guid AssignToUserId { get; init; }
    public string? Remarks { get; init; }
}

public class AssignNdrCaseCommandHandler : IRequestHandler<AssignNdrCaseCommand, Result<NdrDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AssignNdrCaseCommandHandler> _logger;

    public AssignNdrCaseCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<AssignNdrCaseCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<NdrDetailDto>> Handle(
        AssignNdrCaseCommand request,
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
            return Result<NdrDetailDto>.Failure("Cannot assign a closed NDR case");
        }

        // Verify user exists
        var assignee = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.AssignToUserId, cancellationToken);

        if (assignee == null)
        {
            return Result<NdrDetailDto>.Failure("Assigned user not found");
        }

        var previousAssignee = ndr.AssignedToUserId;

        // Assign the case
        ndr.AssignTo(request.AssignToUserId);

        // Add action for assignment
        var actionType = previousAssignee.HasValue
            ? NdrActionType.Reassigned
            : NdrActionType.RemarkAdded;

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        var action = NdrAction.Create(
            ndr.Id,
            actionType,
            currentUserId,
            $"Assigned to {assignee.FullName}",
            null);
        ndr.AddAction(action);

        // Add remark if provided
        if (!string.IsNullOrWhiteSpace(request.Remarks))
        {
            var remark = new NdrRemark(
                ndr.Id,
                currentUserId,
                request.Remarks,
                isInternal: true);
            ndr.AddRemark(remark);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "NDR case {NdrRecordId} assigned to user {UserId} by {AssignedByUserId}",
            ndr.Id, request.AssignToUserId, _currentUserService.UserId);

        // Get related data for response
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ndr.OrderId, cancellationToken);

        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ndr.ShipmentId, cancellationToken);

        // Get user names
        var userIds = ndr.Actions.Select(a => a.PerformedByUserId)
            .Union(ndr.Remarks.Select(r => r.UserId))
            .Append(request.AssignToUserId)
            .Distinct()
            .ToList();

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var dto = MapToDetailDto(ndr, order, shipment, users);
        return Result<NdrDetailDto>.Success(dto);
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
