using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Query to get user details by ID.
/// </summary>
[RequirePermission("team.view")]
[RequireFeature("team")]
public record GetUserByIdQuery : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public Guid UserId { get; init; }
}

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetUserByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UserDetailDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Where(u => u.Id == request.UserId && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return Result<UserDetailDto>.Failure("User not found.");
        }

        // Get assigned by user names
        var assignedByIds = user.UserRoles
            .Where(ur => ur.AssignedBy.HasValue)
            .Select(ur => ur.AssignedBy!.Value)
            .Distinct()
            .ToList();

        var assignedByUsers = await _dbContext.Users
            .AsNoTracking()
            .Where(u => assignedByIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        // Collect all unique permissions from all roles
        var permissions = user.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        var dto = new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Phone = user.Phone,
            IsActive = user.IsActive,
            EmailVerified = user.EmailVerified,
            LastLoginAt = user.LastLoginAt,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockoutEndsAt = user.LockoutEndsAt,
            IsLockedOut = user.IsLockedOut(),
            Roles = user.UserRoles.Select(ur => new UserRoleDto
            {
                RoleId = ur.RoleId,
                RoleName = ur.Role?.Name ?? "Unknown",
                Description = ur.Role?.Description,
                IsSystem = ur.Role?.IsSystem ?? false,
                AssignedAt = ur.AssignedAt,
                AssignedBy = ur.AssignedBy,
                AssignedByName = ur.AssignedBy.HasValue && assignedByUsers.ContainsKey(ur.AssignedBy.Value)
                    ? assignedByUsers[ur.AssignedBy.Value]
                    : null
            }).ToList(),
            Permissions = permissions,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Result<UserDetailDto>.Success(dto);
    }
}
