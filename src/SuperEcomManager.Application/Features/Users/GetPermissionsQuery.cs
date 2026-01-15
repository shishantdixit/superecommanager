using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Query to get all available permissions grouped by module.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team")]
public record GetPermissionsQuery : IRequest<Result<PermissionListDto>>, ITenantRequest
{
}

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, Result<PermissionListDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPermissionsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PermissionListDto>> Handle(
        GetPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);

        var permissionDtos = permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Module = p.Module,
            Description = p.Description
        }).ToList();

        var groupedPermissions = permissions
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

        var result = new PermissionListDto
        {
            TotalCount = permissions.Count,
            Permissions = permissionDtos,
            PermissionsByModule = groupedPermissions
        };

        return Result<PermissionListDto>.Success(result);
    }
}

/// <summary>
/// DTO containing all permissions and grouped permissions.
/// </summary>
public record PermissionListDto
{
    public int TotalCount { get; init; }
    public List<PermissionDto> Permissions { get; init; } = new();
    public List<PermissionGroupDto> PermissionsByModule { get; init; } = new();
}

/// <summary>
/// DTO for permissions grouped by module.
/// </summary>
public record PermissionGroupDto
{
    public string Module { get; init; } = string.Empty;
    public List<PermissionDto> Permissions { get; init; } = new();
}
