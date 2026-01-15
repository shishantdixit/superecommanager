using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Command to assign a role to a user.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team")]
public record AssignRoleCommand : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public Guid? AssignedBy { get; init; }
}

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public AssignRoleCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<UserDetailDto>> Handle(
        AssignRoleCommand request,
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

        // Validate role exists
        var role = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.Id == request.RoleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (role == null)
        {
            return Result<UserDetailDto>.Failure("Role not found.");
        }

        // Check if user already has this role
        if (user.UserRoles.Any(ur => ur.RoleId == request.RoleId))
        {
            return Result<UserDetailDto>.Failure($"User already has the '{role.Name}' role.");
        }

        // Assign role
        user.AssignRole(request.RoleId, request.AssignedBy);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetUserByIdQuery { UserId = request.UserId }, cancellationToken);
    }
}

/// <summary>
/// Command to assign multiple roles to a user at once.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team")]
public record AssignRolesCommand : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public Guid UserId { get; init; }
    public List<Guid> RoleIds { get; init; } = new();
    public Guid? AssignedBy { get; init; }
}

public class AssignRolesCommandHandler : IRequestHandler<AssignRolesCommand, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public AssignRolesCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<UserDetailDto>> Handle(
        AssignRolesCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.RoleIds.Any())
        {
            return Result<UserDetailDto>.Failure("At least one role must be specified.");
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

        var missingRoleIds = request.RoleIds.Except(roles.Select(r => r.Id)).ToList();
        if (missingRoleIds.Any())
        {
            return Result<UserDetailDto>.Failure($"One or more roles not found.");
        }

        // Assign roles
        foreach (var roleId in request.RoleIds)
        {
            user.AssignRole(roleId, request.AssignedBy);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetUserByIdQuery { UserId = request.UserId }, cancellationToken);
    }
}
