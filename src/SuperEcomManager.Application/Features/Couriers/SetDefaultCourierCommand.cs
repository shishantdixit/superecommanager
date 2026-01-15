using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Command to set a courier account as the default.
/// </summary>
[RequirePermission("couriers.update")]
[RequireFeature("courier_management")]
public record SetDefaultCourierCommand : IRequest<Result<bool>>, ITenantRequest
{
    public Guid AccountId { get; init; }
}

public class SetDefaultCourierCommandHandler : IRequestHandler<SetDefaultCourierCommand, Result<bool>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<SetDefaultCourierCommandHandler> _logger;

    public SetDefaultCourierCommandHandler(
        ITenantDbContext dbContext,
        ILogger<SetDefaultCourierCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        SetDefaultCourierCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.AccountId, cancellationToken);

        if (account == null)
        {
            return Result<bool>.Failure("Courier account not found");
        }

        if (!account.IsActive)
        {
            return Result<bool>.Failure("Cannot set inactive courier as default");
        }

        // Remove default from all other accounts
        var allAccounts = await _dbContext.CourierAccounts
            .Where(c => c.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var existing in allAccounts)
        {
            existing.RemoveDefault();
        }

        // Set this account as default
        account.SetAsDefault();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set courier account {AccountId} as default", account.Id);

        return Result<bool>.Success(true);
    }
}
