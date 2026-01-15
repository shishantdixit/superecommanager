using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Command to remove a role from a user.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team")]
public record RemoveRoleCommand : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
}

public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public RemoveRoleCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<UserDetailDto>> Handle(
        RemoveRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Validate user exists
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .Where(u => u.Id == request.UserId && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return Result<UserDetailDto>.Failure("User not found.");
        }

        // Check if user has this role
        var userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == request.RoleId);
        if (userRole == null)
        {
            return Result<UserDetailDto>.Failure("User does not have this role.");
        }

        // Prevent removing the last role if it's an Owner role
        var role = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.Id == request.RoleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (role?.Name == "Owner" && user.UserRoles.Count == 1)
        {
            return Result<UserDetailDto>.Failure("Cannot remove the only Owner role from a user.");
        }

        // Check if this is the last Owner in the tenant
        if (role?.Name == "Owner")
        {
            var ownerCount = await _dbContext.UserRoles
                .AsNoTracking()
                .CountAsync(ur => ur.RoleId == request.RoleId, cancellationToken);

            if (ownerCount <= 1)
            {
                return Result<UserDetailDto>.Failure("Cannot remove the last Owner from the organization.");
            }
        }

        // Remove role
        user.RemoveRole(request.RoleId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetUserByIdQuery { UserId = request.UserId }, cancellationToken);
    }
}

/// <summary>
/// Command to set user's roles (replace all existing roles).
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team")]
public record SetUserRolesCommand : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public Guid UserId { get; init; }
    public List<Guid> RoleIds { get; init; } = new();
    public Guid? AssignedBy { get; init; }
}

public class SetUserRolesCommandHandler : IRequestHandler<SetUserRolesCommand, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public SetUserRolesCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<UserDetailDto>> Handle(
        SetUserRolesCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.RoleIds.Any())
        {
            return Result<UserDetailDto>.Failure("At least one role must be assigned.");
        }

        // Validate user exists
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .Where(u => u.Id == request.UserId && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return Result<UserDetailDto>.Failure("User not found.");
        }

        // Validate all roles exist
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        if (roles.Count != request.RoleIds.Distinct().Count())
        {
            return Result<UserDetailDto>.Failure("One or more roles not found.");
        }

        // Check if we're removing Owner role - prevent if this is the last owner
        var currentOwnerRole = user.UserRoles.FirstOrDefault(ur =>
        {
            var r = _dbContext.Roles.FirstOrDefault(role => role.Id == ur.RoleId);
            return r?.Name == "Owner";
        });

        if (currentOwnerRole != null)
        {
            var ownerRoleInNewSet = roles.FirstOrDefault(r => r.Name == "Owner");
            if (ownerRoleInNewSet == null)
            {
                // Check if this is the last owner
                var ownerCount = await _dbContext.UserRoles
                    .AsNoTracking()
                    .CountAsync(ur => ur.RoleId == currentOwnerRole.RoleId, cancellationToken);

                if (ownerCount <= 1)
                {
                    return Result<UserDetailDto>.Failure("Cannot remove the last Owner from the organization.");
                }
            }
        }

        // Remove all existing roles
        var currentRoles = user.UserRoles.Select(ur => ur.RoleId).ToList();
        foreach (var roleId in currentRoles)
        {
            user.RemoveRole(roleId);
        }

        // Assign new roles
        foreach (var roleId in request.RoleIds.Distinct())
        {
            user.AssignRole(roleId, request.AssignedBy);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetUserByIdQuery { UserId = request.UserId }, cancellationToken);
    }
}
