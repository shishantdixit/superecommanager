using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Ndr;

/// <summary>
/// Query to get a single NDR case by ID with full details.
/// </summary>
[RequirePermission("ndr.view")]
[RequireFeature("ndr_management")]
public record GetNdrCaseByIdQuery : IRequest<Result<NdrDetailDto>>, ITenantRequest
{
    public Guid NdrRecordId { get; init; }
}

public class GetNdrCaseByIdQueryHandler : IRequestHandler<GetNdrCaseByIdQuery, Result<NdrDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetNdrCaseByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<NdrDetailDto>> Handle(
        GetNdrCaseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ndr = await _dbContext.NdrRecords
            .Include(n => n.Actions)
            .Include(n => n.Remarks)
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == request.NdrRecordId, cancellationToken);

        if (ndr == null)
        {
            return Result<NdrDetailDto>.Failure("NDR case not found");
        }

        // Get related entities
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ndr.OrderId, cancellationToken);

        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ndr.ShipmentId, cancellationToken);

        // Get assigned user
        string? assignedUserName = null;
        if (ndr.AssignedToUserId.HasValue)
        {
            var assignedUser = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == ndr.AssignedToUserId.Value, cancellationToken);
            assignedUserName = assignedUser?.FullName;
        }

        // Get user names for actions and remarks
        var userIds = ndr.Actions.Select(a => a.PerformedByUserId)
            .Union(ndr.Remarks.Select(r => r.UserId))
            .Distinct()
            .ToList();

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var dto = new NdrDetailDto
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
            AssignedToUserName = assignedUserName,
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

        return Result<NdrDetailDto>.Success(dto);
    }
}
