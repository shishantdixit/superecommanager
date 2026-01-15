using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Users;

/// <summary>
/// Command to update user profile information.
/// </summary>
[RequirePermission("team.edit")]
[RequireFeature("team")]
public record UpdateUserCommand : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateUserCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<UserDetailDto>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return Result<UserDetailDto>.Failure("First name is required.");
        }

        var user = await _dbContext.Users
            .Where(u => u.Id == request.UserId && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return Result<UserDetailDto>.Failure("User not found.");
        }

        user.UpdateProfile(request.FirstName, request.LastName, request.Phone);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Return updated user details
        return await _mediator.Send(new GetUserByIdQuery { UserId = request.UserId }, cancellationToken);
    }
}

/// <summary>
/// Command to activate a user.
/// </summary>
[RequirePermission("team.edit")]
[RequireFeature("team")]
public record ActivateUserCommand : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public Guid UserId { get; init; }
}

public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public ActivateUserCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<UserDetailDto>> Handle(
        ActivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Where(u => u.Id == request.UserId && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return Result<UserDetailDto>.Failure("User not found.");
        }

        user.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetUserByIdQuery { UserId = request.UserId }, cancellationToken);
    }
}

/// <summary>
/// Command to deactivate a user.
/// </summary>
[RequirePermission("team.edit")]
[RequireFeature("team")]
public record DeactivateUserCommand : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public Guid UserId { get; init; }
}

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public DeactivateUserCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<UserDetailDto>> Handle(
        DeactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Where(u => u.Id == request.UserId && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return Result<UserDetailDto>.Failure("User not found.");
        }

        user.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetUserByIdQuery { UserId = request.UserId }, cancellationToken);
    }
}

/// <summary>
/// Command to unlock a locked-out user.
/// </summary>
[RequirePermission("team.edit")]
[RequireFeature("team")]
public record UnlockUserCommand : IRequest<Result<UserDetailDto>>, ITenantRequest
{
    public Guid UserId { get; init; }
}

public class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, Result<UserDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly IMediator _mediator;

    public UnlockUserCommandHandler(ITenantDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<Result<UserDetailDto>> Handle(
        UnlockUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Where(u => u.Id == request.UserId && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return Result<UserDetailDto>.Failure("User not found.");
        }

        // Clear lockout by recording a successful login
        user.RecordLogin();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetUserByIdQuery { UserId = request.UserId }, cancellationToken);
    }
}
