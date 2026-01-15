using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Query to get paginated list of users.
/// </summary>
[RequirePermission("team.view")]
[RequireFeature("team")]
public record GetUsersQuery : IRequest<Result<PaginatedResult<UserListDto>>>, ITenantRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
    public Guid? RoleId { get; init; }
    public bool? EmailVerified { get; init; }
    public DateTime? LastLoginFrom { get; init; }
    public DateTime? LastLoginTo { get; init; }
    public UserSortBy SortBy { get; init; } = UserSortBy.CreatedAt;
    public bool SortDescending { get; init; } = true;
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PaginatedResult<UserListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetUsersQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<UserListDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.DeletedAt == null)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                (u.Phone != null && u.Phone.Contains(term)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        if (request.EmailVerified.HasValue)
        {
            query = query.Where(u => u.EmailVerified == request.EmailVerified.Value);
        }

        if (request.RoleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == request.RoleId.Value));
        }

        if (request.LastLoginFrom.HasValue)
        {
            query = query.Where(u => u.LastLoginAt >= request.LastLoginFrom.Value);
        }

        if (request.LastLoginTo.HasValue)
        {
            query = query.Where(u => u.LastLoginAt <= request.LastLoginTo.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy switch
        {
            UserSortBy.Name => request.SortDescending
                ? query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName)
                : query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            UserSortBy.Email => request.SortDescending
                ? query.OrderByDescending(u => u.Email)
                : query.OrderBy(u => u.Email),
            UserSortBy.LastLoginAt => request.SortDescending
                ? query.OrderByDescending(u => u.LastLoginAt)
                : query.OrderBy(u => u.LastLoginAt),
            UserSortBy.Status => request.SortDescending
                ? query.OrderByDescending(u => u.IsActive)
                : query.OrderBy(u => u.IsActive),
            _ => request.SortDescending
                ? query.OrderByDescending(u => u.CreatedAt)
                : query.OrderBy(u => u.CreatedAt)
        };

        // Apply pagination
        var users = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var dtos = users.Select(u => new UserListDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            FullName = u.FullName,
            Phone = u.Phone,
            IsActive = u.IsActive,
            EmailVerified = u.EmailVerified,
            LastLoginAt = u.LastLoginAt,
            Roles = u.UserRoles.Select(ur => ur.Role?.Name ?? "Unknown").ToList(),
            CreatedAt = u.CreatedAt
        }).ToList();

        var result = new PaginatedResult<UserListDto>(
            dtos,
            totalCount,
            request.Page,
            request.PageSize);

        return Result<PaginatedResult<UserListDto>>.Success(result);
    }
}
