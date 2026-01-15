using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Identity;

namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Command to invite a new user to the tenant.
/// Creates a user record and optionally sends an invitation email.
/// </summary>
[RequirePermission("team.invite")]
[RequireFeature("team")]
public record InviteUserCommand : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public List<Guid> RoleIds { get; init; } = new();
    public Guid? InvitedBy { get; init; }
    public bool SendInvitationEmail { get; init; } = true;
}

public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public InviteUserCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<UserDetailDto>> Handle(
        InviteUserCommand request,
        CancellationToken cancellationToken)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<UserDetailDto>.Failure("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return Result<UserDetailDto>.Failure("First name is required.");
        }

        // Normalize email
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check if email already exists (including soft-deleted)
        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Email == normalizedEmail)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser != null)
        {
            if (existingUser.DeletedAt == null)
            {
                return Result<UserDetailDto>.Failure("A user with this email already exists.");
            }
            else
            {
                return Result<UserDetailDto>.Failure("A user with this email was previously removed. Please contact support to restore the account.");
            }
        }

        // Validate roles exist
        if (request.RoleIds.Any())
        {
            var roles = await _dbContext.Roles
                .AsNoTracking()
                .Where(r => request.RoleIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            if (roles.Count != request.RoleIds.Distinct().Count())
            {
                return Result<UserDetailDto>.Failure("One or more specified roles not found.");
            }
        }

        // Create user with a temporary password hash
        // In a real implementation, this would generate an invitation token
        // and allow the user to set their password via a registration link
        var temporaryPasswordHash = GenerateTemporaryPasswordHash();

        var user = User.Create(
            normalizedEmail,
            request.FirstName.Trim(),
            request.LastName,
            temporaryPasswordHash,
            request.Phone);

        // Deactivate until they complete registration
        user.Deactivate();

        // Assign roles
        foreach (var roleId in request.RoleIds.Distinct())
        {
            user.AssignRole(roleId, request.InvitedBy);
        }

        // If no roles specified, assign default Viewer role
        if (!request.RoleIds.Any())
        {
            var viewerRole = await _dbContext.Roles
                .AsNoTracking()
                .Where(r => r.Name == "Viewer")
                .FirstOrDefaultAsync(cancellationToken);

            if (viewerRole != null)
            {
                user.AssignRole(viewerRole.Id, request.InvitedBy);
            }
        }

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // TODO: Send invitation email if SendInvitationEmail is true
        // This would be handled by a notification service
        // await _notificationService.SendInvitationEmail(user, request.InvitedBy);

        return await _mediator.Send(new GetUserByIdQuery { UserId = user.Id }, cancellationToken);
    }

    private static string GenerateTemporaryPasswordHash()
    {
        // Generate a secure random placeholder that cannot be used for login
        // The user will set their own password via the invitation link
        return $"INVITE_{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}";
    }
}

/// <summary>
/// Command to delete (soft-delete) a user.
/// </summary>
[RequirePermission("team.delete")]
[RequireFeature("team")]
public record DeleteUserCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid UserId { get; init; }
    public Guid? DeletedBy { get; init; }
}

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;

    public DeleteUserCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .Where(u => u.Id == request.UserId && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("User not found.");
        }

        // Check if user is the last Owner
        var ownerRole = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.Name == "Owner")
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerRole != null && user.UserRoles.Any(ur => ur.RoleId == ownerRole.Id))
        {
            var ownerCount = await _dbContext.UserRoles
                .AsNoTracking()
                .CountAsync(ur => ur.RoleId == ownerRole.Id, cancellationToken);

            if (ownerCount <= 1)
            {
                return Result<bool>.Failure("Cannot delete the last Owner of the organization.");
            }
        }

        // Prevent self-deletion
        if (request.DeletedBy.HasValue && request.UserId == request.DeletedBy.Value)
        {
            return Result<bool>.Failure("You cannot delete your own account.");
        }

        // Soft delete
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = request.DeletedBy;
        user.Deactivate();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
