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
/// Command to schedule a reattempt for an NDR case.
/// </summary>
[RequirePermission("ndr.edit")]
[RequireFeature("ndr_management")]
public record ScheduleReattemptCommand : IRequest<Result<NdrDetailDto>>, ITenantRequest
{
    public Guid NdrRecordId { get; init; }
    public DateTime ReattemptDate { get; init; }
    public AddressDto? UpdatedAddress { get; init; }
    public string? Remarks { get; init; }
}

public class ScheduleReattemptCommandHandler : IRequestHandler<ScheduleReattemptCommand, Result<NdrDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ScheduleReattemptCommandHandler> _logger;

    public ScheduleReattemptCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<ScheduleReattemptCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<NdrDetailDto>> Handle(
        ScheduleReattemptCommand request,
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
            return Result<NdrDetailDto>.Failure("Cannot schedule reattempt for a closed NDR case");
        }

        // Validate reattempt date
        if (request.ReattemptDate <= DateTime.UtcNow)
        {
            return Result<NdrDetailDto>.Failure("Reattempt date must be in the future");
        }

        // Get shipment for response DTO
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(s => s.Id == ndr.ShipmentId, cancellationToken);

        // Schedule reattempt
        ndr.ScheduleReattempt(request.ReattemptDate);

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        // Log address update request if provided (address update on shipment would require courier integration)
        if (request.UpdatedAddress != null)
        {
            // Add address update action
            var addressAction = NdrAction.Create(
                ndr.Id,
                NdrActionType.AddressUpdated,
                currentUserId,
                $"Address update requested: {request.UpdatedAddress.Line1}, {request.UpdatedAddress.City}, {request.UpdatedAddress.State} - {request.UpdatedAddress.PostalCode}",
                null);
            ndr.AddAction(addressAction);
        }

        // Add reattempt action
        var action = NdrAction.Create(
            ndr.Id,
            NdrActionType.ReattemptRequested,
            currentUserId,
            $"Reattempt scheduled for {request.ReattemptDate:yyyy-MM-dd}",
            request.Remarks);
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
            "Reattempt scheduled for NDR case {NdrRecordId} on {ReattemptDate} by user {UserId}",
            ndr.Id, request.ReattemptDate, _currentUserService.UserId);

        // Get related data for response
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ndr.OrderId, cancellationToken);

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
