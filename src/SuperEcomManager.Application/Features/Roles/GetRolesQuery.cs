using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Application.Features.Roles;

/// <summary>
/// Query to get all roles for the current tenant.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team_management")]
public record GetRolesQuery : IRequest<IReadOnlyList<RoleSummaryDto>>, ITenantRequest;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleSummaryDto>>
{
    private readonly ITenantDbContext _dbContext;

    public GetRolesQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RoleSummaryDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Select(r => new RoleSummaryDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                UserCount = _dbContext.UserRoles.Count(ur => ur.RoleId == r.Id),
                PermissionCount = r.RolePermissions.Count
            })
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles;
    }
}
