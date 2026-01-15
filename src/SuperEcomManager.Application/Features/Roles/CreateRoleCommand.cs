using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Identity;

namespace SuperEcomManager.Application.Features.Roles;

/// <summary>
/// Command to create a new role.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team_management")]
public record CreateRoleCommand : IRequest<Result<RoleDto>>, ITenantRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<Guid> PermissionIds { get; init; } = Array.Empty<Guid>();
}

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(100).WithMessage("Role name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    private readonly ITenantDbContext _tenantDb;
    private readonly IApplicationDbContext _applicationDb;

    public CreateRoleCommandHandler(ITenantDbContext tenantDb, IApplicationDbContext applicationDb)
    {
        _tenantDb = tenantDb;
        _applicationDb = applicationDb;
    }

    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if role name already exists
        var existingRole = await _tenantDb.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == request.Name, cancellationToken);

        if (existingRole != null)
        {
            return Result<RoleDto>.Failure("A role with this name already exists");
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

        // Create role
        var role = Role.Create(request.Name, request.Description);

        // Add permissions
        foreach (var permissionId in request.PermissionIds)
        {
            role.AddPermission(permissionId);
        }

        await _tenantDb.Roles.AddAsync(role, cancellationToken);
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
            .ToListAsync(cancellationToken);

        return Result<RoleDto>.Success(new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            UserCount = 0,
            Permissions = permissions,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        });
    }
}
