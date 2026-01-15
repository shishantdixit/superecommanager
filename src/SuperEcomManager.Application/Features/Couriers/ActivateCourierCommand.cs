using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Command to activate a courier account.
/// </summary>
[RequirePermission("couriers.update")]
[RequireFeature("courier_management")]
public record ActivateCourierCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid AccountId { get; init; }
}

public class ActivateCourierCommandHandler : IRequestHandler<ActivateCourierCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<ActivateCourierCommandHandler> _logger;

    public ActivateCourierCommandHandler(
        ITenantDbContext dbContext,
        ILogger<ActivateCourierCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        ActivateCourierCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.AccountId, cancellationToken);

        if (account == null)
        {
            return Result<bool>.Failure("Courier account not found");
        }

        try
        {
            account.Activate();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Activated courier account {AccountId}", account.Id);
            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }
    }
}

/// <summary>
/// Command to deactivate a courier account.
/// </summary>
[RequirePermission("couriers.update")]
[RequireFeature("courier_management")]
public record DeactivateCourierCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid AccountId { get; init; }
}

public class DeactivateCourierCommandHandler : IRequestHandler<DeactivateCourierCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<DeactivateCourierCommandHandler> _logger;

    public DeactivateCourierCommandHandler(
        ITenantDbContext dbContext,
        ILogger<DeactivateCourierCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        DeactivateCourierCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.AccountId, cancellationToken);

        if (account == null)
        {
            return Result<bool>.Failure("Courier account not found");
        }

        if (account.IsDefault)
        {
            return Result<bool>.Failure("Cannot deactivate the default courier. Set another courier as default first.");
        }

        account.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated courier account {AccountId}", account.Id);
        return Result<bool>.Success(true);
    }
}
