using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Exceptions;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Roles;

/// <summary>
/// Command to delete a role.
/// </summary>
[RequirePermission("team.roles")]
[RequireFeature("team_management")]
public record DeleteRoleCommand(Guid Id) : IRequest<Result<bool>>, ITenantRequest;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;

    public DeleteRoleCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException("Role", request.Id);
        }

        if (role.IsSystem)
        {
            return Result<bool>.Failure("System roles cannot be deleted");
        }

        // Check if role is assigned to any users
        var usersWithRole = await _dbContext.UserRoles
            .CountAsync(ur => ur.RoleId == request.Id, cancellationToken);

        if (usersWithRole > 0)
        {
            return Result<bool>.Failure($"Cannot delete role. It is assigned to {usersWithRole} user(s)");
        }

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
