using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Features.Roles;

namespace SuperEcomManager.Application.Features.Permissions;

/// <summary>
/// Query to get all permissions grouped by module.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team_management")]
public record GetPermissionsQuery : IRequest<IReadOnlyList<PermissionGroupDto>>, ITenantRequest;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionGroupDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPermissionsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PermissionGroupDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var grouped = permissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto
            {
                Module = g.Key,
                Permissions = g.Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Module = p.Module,
                    Description = p.Description
                }).ToList()
            })
            .OrderBy(g => g.Module)
            .ToList();

        return grouped;
    }
}
