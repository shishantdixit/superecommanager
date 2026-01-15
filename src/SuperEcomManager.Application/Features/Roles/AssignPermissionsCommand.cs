using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Exceptions;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Roles;

/// <summary>
/// Command to assign permissions to a role.
/// Replaces all existing permissions with the provided list.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team_management")]
public record AssignPermissionsCommand : IRequest<Result<RoleDto>>, ITenantRequest
{
    public Guid RoleId { get; init; }
    public IReadOnlyList<Guid> PermissionIds { get; init; } = Array.Empty<Guid>();
}

public class AssignPermissionsCommandHandler : IRequestHandler<AssignPermissionsCommand, Result<RoleDto>>
{
    private readonly ITenantDbContext _tenantDb;
    private readonly IApplicationDbContext _applicationDb;

    public AssignPermissionsCommandHandler(ITenantDbContext tenantDb, IApplicationDbContext applicationDb)
    {
        _tenantDb = tenantDb;
        _applicationDb = applicationDb;
    }

    public async Task<Result<RoleDto>> Handle(AssignPermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _tenantDb.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException("Role", request.RoleId);
        }

        // Validate permission IDs exist
        if (request.PermissionIds.Any())
        {
            var validPermissionIds = await _applicationDb.Permissions
                .AsNoTracking()
                .Where(p => request.PermissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var invalidIds = request.PermissionIds.Except(validPermissionIds).ToList();
            if (invalidIds.Any())
            {
                return Result<RoleDto>.Failure($"Invalid permission IDs: {string.Join(", ", invalidIds)}");
            }
        }

        // Replace all permissions
        role.SetPermissions(request.PermissionIds);
        await _tenantDb.SaveChangesAsync(cancellationToken);

        // Get permission details
        var permissions = await _applicationDb.Permissions
            .AsNoTracking()
            .Where(p => request.PermissionIds.Contains(p.Id))
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
            .CountAsync(ur => ur.RoleId == request.RoleId, cancellationToken);

        return Result<RoleDto>.Success(new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            UserCount = userCount,
            Permissions = permissions,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        });
    }
}
