using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Query to get all roles.
/// </summary>
[RequirePermission("team.view")]
[RequireFeature("team")]
public record GetRolesQuery : IRequest<Result<List<RoleListDto>>>, ITenantRequest
{
}

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<RoleListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetRolesQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<RoleListDto>>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .OrderBy(r => r.IsSystem ? 0 : 1)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        // Get user counts per role
        var userCounts = await _dbContext.UserRoles
            .AsNoTracking()
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var dtos = roles.Select(r => new RoleListDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystem = r.IsSystem,
            UserCount = userCounts.GetValueOrDefault(r.Id, 0),
            PermissionCount = r.RolePermissions.Count,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Result<List<RoleListDto>>.Success(dtos);
    }
}

/// <summary>
/// Query to get role details by ID.
/// </summary>
[RequirePermission("team.view")]
[RequireFeature("team")]
public record GetRoleByIdQuery : IRequest<Result<RoleDto>>, ITenantRequest
{
    public Guid RoleId { get; init; }
}

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetRoleByIdQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<RoleDto>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.Id == request.RoleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (role == null)
        {
            return Result<RoleDto>.Failure("Role not found.");
        }

        var userCount = await _dbContext.UserRoles
            .AsNoTracking()
            .CountAsync(ur => ur.RoleId == request.RoleId, cancellationToken);

        var dto = new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            UserCount = userCount,
            Permissions = role.RolePermissions
                .Where(rp => rp.Permission != null)
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission!.Id,
                    Code = rp.Permission.Code,
                    Name = rp.Permission.Name,
                    Module = rp.Permission.Module,
                    Description = rp.Permission.Description
                })
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToList(),
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };

        return Result<RoleDto>.Success(dto);
    }
}
