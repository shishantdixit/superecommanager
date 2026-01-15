using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Ndr;

/// <summary>
/// Query to get NDR statistics.
/// </summary>
[RequirePermission("ndr.view")]
[RequireFeature("ndr_management")]
public record GetNdrStatsQuery : IRequest<Result<NdrStatsDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class GetNdrStatsQueryHandler : IRequestHandler<GetNdrStatsQuery, Result<NdrStatsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetNdrStatsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<NdrStatsDto>> Handle(
        GetNdrStatsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.NDR.NdrRecord> query = _dbContext.NdrRecords
            .AsNoTracking();

        if (request.FromDate.HasValue)
            query = query.Where(n => n.NdrDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(n => n.NdrDate <= request.ToDate.Value);

        var allRecords = await query.ToListAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Count by status
        var openCount = allRecords.Count(n => n.Status == NdrStatus.Open);
        var assignedCount = allRecords.Count(n => n.Status == NdrStatus.Assigned);
        var reattemptScheduledCount = allRecords.Count(n => n.Status == NdrStatus.ReattemptScheduled);
        var deliveredCount = allRecords.Count(n => n.Status == NdrStatus.Delivered || n.Status == NdrStatus.ClosedDelivered);
        var rtoCount = allRecords.Count(n =>
            n.Status == NdrStatus.RTOInitiated ||
            n.Status == NdrStatus.ClosedRTO);

        // Pending follow-up (next follow-up is due)
        var pendingFollowUpCount = allRecords.Count(n =>
            n.NextFollowUpAt.HasValue &&
            n.NextFollowUpAt.Value <= DateTime.UtcNow &&
            n.Status != NdrStatus.ClosedDelivered &&
            n.Status != NdrStatus.ClosedRTO &&
            n.Status != NdrStatus.ClosedAddressUpdated);

        // Today's stats
        var closedToday = allRecords.Count(n =>
            n.ResolvedAt.HasValue &&
            n.ResolvedAt.Value >= today &&
            n.ResolvedAt.Value < tomorrow);

        var openedToday = allRecords.Count(n =>
            n.NdrDate >= today &&
            n.NdrDate < tomorrow);

        // By reason code
        var byReasonCode = allRecords
            .GroupBy(n => n.ReasonCode)
            .ToDictionary(g => g.Key, g => g.Count());

        // By assignee
        var assignedRecords = allRecords.Where(n => n.AssignedToUserId.HasValue).ToList();
        var userIds = assignedRecords.Select(n => n.AssignedToUserId!.Value).Distinct().ToList();

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var byAssignee = assignedRecords
            .GroupBy(n => n.AssignedToUserId!.Value)
            .ToDictionary(
                g => users.GetValueOrDefault(g.Key, "Unknown"),
                g => g.Count());

        // Calculate success rate
        var totalResolved = allRecords.Count(n =>
            n.Status == NdrStatus.ClosedDelivered ||
            n.Status == NdrStatus.ClosedRTO ||
            n.Status == NdrStatus.ClosedAddressUpdated);

        var successfullyDelivered = allRecords.Count(n =>
            n.Status == NdrStatus.ClosedDelivered ||
            n.Status == NdrStatus.ClosedAddressUpdated);

        var deliverySuccessRate = totalResolved > 0
            ? Math.Round((decimal)successfullyDelivered / totalResolved * 100, 2)
            : 0;

        // Calculate average resolution time
        var resolvedRecords = allRecords
            .Where(n => n.ResolvedAt.HasValue)
            .ToList();

        var avgResolutionHours = resolvedRecords.Count > 0
            ? Math.Round((decimal)resolvedRecords.Average(n =>
                (n.ResolvedAt!.Value - n.NdrDate).TotalHours), 2)
            : 0;

        var stats = new NdrStatsDto
        {
            TotalOpen = openCount,
            TotalAssigned = assignedCount,
            TotalPendingFollowUp = pendingFollowUpCount,
            TotalReattemptScheduled = reattemptScheduledCount,
            TotalDelivered = deliveredCount,
            TotalRTO = rtoCount,
            TotalClosedToday = closedToday,
            TotalOpenedToday = openedToday,
            ByReasonCode = byReasonCode,
            ByAssignee = byAssignee,
            DeliverySuccessRate = deliverySuccessRate,
            AverageResolutionHours = avgResolutionHours
        };

        return Result<NdrStatsDto>.Success(stats);
    }
}
