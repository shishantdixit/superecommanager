using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Exceptions;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Application.Features.Roles;

/// <summary>
/// Query to get a role by ID with its permissions.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team_management")]
public record GetRoleByIdQuery(Guid Id) : IRequest<RoleDto>, ITenantRequest;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly ITenantDbContext _tenantDb;
    private readonly IApplicationDbContext _applicationDb;

    public GetRoleByIdQueryHandler(ITenantDbContext tenantDb, IApplicationDbContext applicationDb)
    {
        _tenantDb = tenantDb;
        _applicationDb = applicationDb;
    }

    public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _tenantDb.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException("Role", request.Id);
        }

        // Get permission details from shared schema
        var permissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList();
        var permissions = await _applicationDb.Permissions
            .AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Module = p.Module,
                Description = p.Description
            })
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var userCount = await _tenantDb.UserRoles
            .CountAsync(ur => ur.RoleId == request.Id, cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            UserCount = userCount,
            Permissions = permissions,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}
