using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Notifications;

/// <summary>
/// Query to get notification statistics.
/// </summary>
[RequirePermission("notifications.stats.view")]
[RequireFeature("notifications")]
public record GetNotificationStatsQuery : IRequest<Result<NotificationStatsDto>>, ITenantRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool IncludeDailyStats { get; init; } = true;
    public int DailyStatsDays { get; init; } = 30;
}

public class GetNotificationStatsQueryHandler : IRequestHandler<GetNotificationStatsQuery, Result<NotificationStatsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetNotificationStatsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<NotificationStatsDto>> Handle(
        GetNotificationStatsQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;

        var logs = await _dbContext.NotificationLogs
            .AsNoTracking()
            .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        // Overall stats
        var totalSent = logs.Count(l => l.Status == "Sent" || l.Status == "Delivered");
        var totalDelivered = logs.Count(l => l.Status == "Delivered");
        var totalFailed = logs.Count(l => l.Status == "Failed");
        var totalPending = logs.Count(l => l.Status == "Pending");
        var deliveryRate = totalSent > 0 ? (decimal)totalDelivered / totalSent * 100 : 0;

        // By type
        var countByType = logs
            .GroupBy(l => l.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // By status
        var countByStatus = logs
            .GroupBy(l => l.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Type stats
        var typeStats = Enum.GetValues<NotificationType>()
            .Select(type =>
            {
                var typeLogs = logs.Where(l => l.Type == type).ToList();
                var typeSent = typeLogs.Count(l => l.Status == "Sent" || l.Status == "Delivered");
                var typeDelivered = typeLogs.Count(l => l.Status == "Delivered");
                var typeFailed = typeLogs.Count(l => l.Status == "Failed");
                var typeDeliveryRate = typeSent > 0 ? (decimal)typeDelivered / typeSent * 100 : 0;

                return new NotificationTypeStatsDto
                {
                    Type = type,
                    TotalSent = typeSent,
                    Delivered = typeDelivered,
                    Failed = typeFailed,
                    DeliveryRate = Math.Round(typeDeliveryRate, 2)
                };
            })
            .Where(s => s.TotalSent > 0 || s.Failed > 0)
            .ToList();

        // Daily stats
        var dailyStats = new List<DailyNotificationStatsDto>();
        if (request.IncludeDailyStats)
        {
            var trendStartDate = DateTime.UtcNow.AddDays(-request.DailyStatsDays).Date;
            var trendEndDate = DateTime.UtcNow.Date;

            var dailyData = logs
                .Where(l => l.CreatedAt.Date >= trendStartDate)
                .GroupBy(l => l.CreatedAt.Date)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Sent = g.Count(l => l.Status == "Sent" || l.Status == "Delivered"),
                        Delivered = g.Count(l => l.Status == "Delivered"),
                        Failed = g.Count(l => l.Status == "Failed")
                    });

            // Fill in missing days
            for (var date = trendStartDate; date <= trendEndDate; date = date.AddDays(1))
            {
                var dayData = dailyData.GetValueOrDefault(date);
                dailyStats.Add(new DailyNotificationStatsDto
                {
                    Date = date,
                    Sent = dayData?.Sent ?? 0,
                    Delivered = dayData?.Delivered ?? 0,
                    Failed = dayData?.Failed ?? 0
                });
            }
        }

        var stats = new NotificationStatsDto
        {
            TotalSent = totalSent,
            TotalDelivered = totalDelivered,
            TotalFailed = totalFailed,
            TotalPending = totalPending,
            DeliveryRate = Math.Round(deliveryRate, 2),
            CountByType = countByType,
            CountByStatus = countByStatus,
            DailyStats = dailyStats,
            TypeStats = typeStats
        };

        return Result<NotificationStatsDto>.Success(stats);
    }
}
