using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Query to get user statistics.
/// </summary>
[RequirePermission("team.view")]
[RequireFeature("team")]
public record GetUserStatsQuery : IRequest<Result<UserStatsDto>>, ITenantRequest
{
}

public class GetUserStatsQueryHandler : IRequestHandler<GetUserStatsQuery, Result<UserStatsDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetUserStatsQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UserStatsDto>> Handle(
        GetUserStatsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.AddDays(-(int)now.DayOfWeek).Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var totalUsers = users.Count;
        var activeUsers = users.Count(u => u.IsActive);
        var inactiveUsers = totalUsers - activeUsers;
        var verifiedUsers = users.Count(u => u.EmailVerified);

        var usersLoggedInToday = users.Count(u => u.LastLoginAt >= todayStart);
        var usersLoggedInThisWeek = users.Count(u => u.LastLoginAt >= weekStart);
        var usersLoggedInThisMonth = users.Count(u => u.LastLoginAt >= monthStart);

        // Users by role
        var usersByRole = users
            .SelectMany(u => u.UserRoles)
            .Where(ur => ur.Role != null)
            .GroupBy(ur => ur.Role!.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        // Recent active users
        var recentUsers = users
            .Where(u => u.LastLoginAt.HasValue)
            .OrderByDescending(u => u.LastLoginAt)
            .Take(10)
            .Select(u => new RecentUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                LastLoginAt = u.LastLoginAt
            })
            .ToList();

        var stats = new UserStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            InactiveUsers = inactiveUsers,
            VerifiedUsers = verifiedUsers,
            UsersLoggedInToday = usersLoggedInToday,
            UsersLoggedInThisWeek = usersLoggedInThisWeek,
            UsersLoggedInThisMonth = usersLoggedInThisMonth,
            UsersByRole = usersByRole,
            RecentUsers = recentUsers
        };

        return Result<UserStatsDto>.Success(stats);
    }
}
