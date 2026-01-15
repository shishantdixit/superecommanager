using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Analytics;

/// <summary>
/// Query to get NDR analytics with resolution rates.
/// </summary>
[RequirePermission("analytics.view")]
[RequireFeature("analytics")]
public record GetNdrAnalyticsQuery : IRequest<Result<NdrAnalyticsDto>>, ITenantRequest
{
    public AnalyticsPeriod Period { get; init; } = AnalyticsPeriod.Last30Days;
    public DateTime? CustomStartDate { get; init; }
    public DateTime? CustomEndDate { get; init; }
}

public class GetNdrAnalyticsQueryHandler : IRequestHandler<GetNdrAnalyticsQuery, Result<NdrAnalyticsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetNdrAnalyticsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<NdrAnalyticsDto>> Handle(
        GetNdrAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var (startDate, endDate, _, _) = AnalyticsPeriodHelper.GetDateRange(
            request.Period,
            request.CustomStartDate,
            request.CustomEndDate);

        // Get NDR records
        var ndrRecords = await _dbContext.NdrRecords
            .AsNoTracking()
            .Where(n => n.CreatedAt >= startDate && n.CreatedAt <= endDate)
            .Select(n => new
            {
                n.Id,
                n.Status,
                n.ReasonCode,
                n.ReasonDescription,
                n.CreatedAt,
                n.ResolvedAt,
                n.AssignedToUserId
            })
            .ToListAsync(cancellationToken);

        // Get NDR actions for call stats
        var ndrIds = ndrRecords.Select(n => n.Id).ToList();
        var ndrActions = await _dbContext.NdrActions
            .AsNoTracking()
            .Where(a => ndrIds.Contains(a.NdrRecordId))
            .Select(a => new
            {
                a.NdrRecordId,
                a.ActionType,
                a.PerformedByUserId,
                a.Outcome
            })
            .ToListAsync(cancellationToken);

        // Get user names for agents
        var agentIds = ndrRecords
            .Where(n => n.AssignedToUserId.HasValue)
            .Select(n => n.AssignedToUserId!.Value)
            .Distinct()
            .ToList();

        var agents = await _dbContext.Users
            .AsNoTracking()
            .Where(u => agentIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(cancellationToken);

        var totalNdrCases = ndrRecords.Count;
        var resolvedStatuses = new[] { NdrStatus.ClosedDelivered, NdrStatus.ClosedRTO, NdrStatus.ClosedAddressUpdated, NdrStatus.Delivered };
        var resolvedCount = ndrRecords.Count(n => resolvedStatuses.Contains(n.Status));
        var pendingStatuses = new[] { NdrStatus.Open, NdrStatus.Assigned, NdrStatus.CustomerContacted, NdrStatus.ReattemptScheduled, NdrStatus.ReattemptInProgress };
        var pendingCount = ndrRecords.Count(n => pendingStatuses.Contains(n.Status));
        var escalatedCount = ndrRecords.Count(n => n.Status == NdrStatus.Escalated);

        var resolutionRate = totalNdrCases > 0 ? ((decimal)resolvedCount / totalNdrCases) * 100 : 0;

        // Average resolution time
        var resolvedWithTime = ndrRecords
            .Where(n => resolvedStatuses.Contains(n.Status) && n.ResolvedAt.HasValue)
            .Select(n => (n.ResolvedAt!.Value - n.CreatedAt).TotalHours)
            .ToList();

        var avgResolutionHours = resolvedWithTime.Any() ? (decimal)resolvedWithTime.Average() : 0;

        // By reason
        var byReason = ndrRecords
            .GroupBy(n => n.ReasonCode)
            .Select(g =>
            {
                var reasonResolved = g.Count(n => resolvedStatuses.Contains(n.Status));
                return new NdrReasonBreakdownDto
                {
                    ReasonCode = g.Key.ToString(),
                    ReasonDescription = GetReasonDescription(g.Key),
                    Count = g.Count(),
                    Percentage = totalNdrCases > 0 ? ((decimal)g.Count() / totalNdrCases) * 100 : 0,
                    ResolutionRate = g.Count() > 0 ? ((decimal)reasonResolved / g.Count()) * 100 : 0
                };
            })
            .OrderByDescending(r => r.Count)
            .ToList();

        // By status
        var byStatus = ndrRecords
            .GroupBy(n => n.Status)
            .Select(g => new NdrStatusBreakdownDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                Percentage = totalNdrCases > 0 ? ((decimal)g.Count() / totalNdrCases) * 100 : 0
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        // Daily NDR
        var dailyNdr = ndrRecords
            .GroupBy(n => n.CreatedAt.Date)
            .Select(g => new DailyNdrDto
            {
                Date = g.Key,
                NewCases = g.Count(),
                ResolvedCases = g.Count(n => resolvedStatuses.Contains(n.Status)),
                EscalatedCases = g.Count(n => n.Status == NdrStatus.Escalated)
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Agent performance
        var agentPerformance = ndrRecords
            .Where(n => n.AssignedToUserId.HasValue)
            .GroupBy(n => n.AssignedToUserId!.Value)
            .Select(g =>
            {
                var agent = agents.FirstOrDefault(a => a.Id == g.Key);
                var agentNdrIds = g.Select(n => n.Id).ToList();
                var agentActions = ndrActions.Where(a => agentNdrIds.Contains(a.NdrRecordId)).ToList();
                var calls = agentActions.Count(a => a.ActionType == NdrActionType.PhoneCall);
                var successfulContacts = agentActions.Count(a =>
                    a.ActionType == NdrActionType.PhoneCall &&
                    !string.IsNullOrEmpty(a.Outcome) &&
                    a.Outcome.Contains("connected", StringComparison.OrdinalIgnoreCase));

                var agentResolved = g.Where(n => resolvedStatuses.Contains(n.Status) && n.ResolvedAt.HasValue).ToList();
                var agentAvgResolution = agentResolved.Any()
                    ? (decimal)agentResolved.Average(n => (n.ResolvedAt!.Value - n.CreatedAt).TotalHours)
                    : 0;

                return new AgentPerformanceDto
                {
                    AgentId = g.Key,
                    AgentName = agent?.Name ?? "Unknown",
                    AssignedCases = g.Count(),
                    ResolvedCases = g.Count(n => resolvedStatuses.Contains(n.Status)),
                    PendingCases = g.Count(n => pendingStatuses.Contains(n.Status)),
                    ResolutionRate = g.Count() > 0 ? Math.Round(((decimal)g.Count(n => resolvedStatuses.Contains(n.Status)) / g.Count()) * 100, 2) : 0,
                    AverageResolutionHours = Math.Round(agentAvgResolution, 1),
                    TotalCalls = calls,
                    SuccessfulContacts = successfulContacts
                };
            })
            .OrderByDescending(a => a.ResolvedCases)
            .ToList();

        var result = new NdrAnalyticsDto
        {
            TotalNdrCases = totalNdrCases,
            ResolvedCount = resolvedCount,
            PendingCount = pendingCount,
            EscalatedCount = escalatedCount,
            ResolutionRate = Math.Round(resolutionRate, 2),
            AverageResolutionHours = Math.Round(avgResolutionHours, 1),
            ByReason = byReason,
            ByStatus = byStatus,
            DailyNdr = dailyNdr,
            AgentPerformance = agentPerformance
        };

        return Result<NdrAnalyticsDto>.Success(result);
    }

    private static string GetReasonDescription(NdrReasonCode code)
    {
        return code switch
        {
            NdrReasonCode.CustomerNotAvailable => "Customer Not Available",
            NdrReasonCode.CustomerRefused => "Customer Refused",
            NdrReasonCode.IncorrectAddress => "Address Incomplete/Incorrect",
            NdrReasonCode.FutureDeliveryRequested => "Future Delivery Requested",
            NdrReasonCode.CustomerUnreachable => "Customer Unreachable",
            NdrReasonCode.PremisesClosed => "Premises Closed",
            NdrReasonCode.CustomerOutOfStation => "Customer Out of Station",
            NdrReasonCode.CODNotReady => "COD Amount Not Ready",
            NdrReasonCode.AddressChangeRequested => "Address Change Requested",
            NdrReasonCode.ProductDamaged => "Product Damaged",
            NdrReasonCode.OpenDeliveryRequested => "Open Delivery Requested",
            NdrReasonCode.SecurityRestriction => "Security Restriction",
            NdrReasonCode.WeatherIssue => "Weather Issue",
            NdrReasonCode.Other => "Other",
            _ => code.ToString()
        };
    }
}
