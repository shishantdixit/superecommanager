using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Exceptions;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Roles;

/// <summary>
/// Command to update an existing role.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team_management")]
public record UpdateRoleCommand : IRequest<Result<RoleDto>>, ITenantRequest
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(100).WithMessage("Role name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
}

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<RoleDto>>
{
    private readonly ITenantDbContext _tenantDb;
    private readonly IApplicationDbContext _applicationDb;

    public UpdateRoleCommandHandler(ITenantDbContext tenantDb, IApplicationDbContext applicationDb)
    {
        _tenantDb = tenantDb;
        _applicationDb = applicationDb;
    }

    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _tenantDb.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException("Role", request.Id);
        }

        if (role.IsSystem)
        {
            return Result<RoleDto>.Failure("System roles cannot be modified");
        }

        // Check if name is taken by another role
        var existingRole = await _tenantDb.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == request.Name && r.Id != request.Id, cancellationToken);

        if (existingRole != null)
        {
            return Result<RoleDto>.Failure("A role with this name already exists");
        }

        // Update role
        role.Update(request.Name, request.Description);
        await _tenantDb.SaveChangesAsync(cancellationToken);

        // Get permission details
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
            .ToListAsync(cancellationToken);

        var userCount = await _tenantDb.UserRoles
            .CountAsync(ur => ur.RoleId == request.Id, cancellationToken);

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
