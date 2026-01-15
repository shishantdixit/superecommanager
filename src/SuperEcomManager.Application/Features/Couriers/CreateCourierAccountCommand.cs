using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Shipping;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Command to create a new courier account.
/// </summary>
[RequirePermission("couriers.create")]
[RequireFeature("courier_management")]
public record CreateCourierAccountCommand : IRequest<Result<CourierAccountDto>>, ITenantRequest
{
    public string Name { get; init; } = string.Empty;
    public CourierType CourierType { get; init; }
    public bool IsDefault { get; init; }
    public int Priority { get; init; } = 100;
}

public class CreateCourierAccountCommandHandler : IRequestHandler<CreateCourierAccountCommand, Result<CourierAccountDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<CreateCourierAccountCommandHandler> _logger;

    public CreateCourierAccountCommandHandler(
        ITenantDbContext dbContext,
        ILogger<CreateCourierAccountCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CourierAccountDto>> Handle(
        CreateCourierAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Validate name is unique
        var nameExists = await _dbContext.CourierAccounts
            .AnyAsync(c => c.Name == request.Name, cancellationToken);

        if (nameExists)
        {
            return Result<CourierAccountDto>.Failure("A courier account with this name already exists");
        }

        // If this is marked as default, remove default from other accounts of same type
        if (request.IsDefault)
        {
            var existingDefaults = await _dbContext.CourierAccounts
                .Where(c => c.CourierType == request.CourierType && c.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.RemoveDefault();
            }
        }

        // Create the courier account
        var account = CourierAccount.Create(
            request.Name,
            request.CourierType,
            request.IsDefault,
            request.Priority);

        await _dbContext.CourierAccounts.AddAsync(account, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created courier account {AccountId} of type {CourierType}",
            account.Id, request.CourierType);

        return Result<CourierAccountDto>.Success(new CourierAccountDto
        {
            Id = account.Id,
            Name = account.Name,
            CourierType = account.CourierType,
            IsActive = account.IsActive,
            IsDefault = account.IsDefault,
            IsConnected = account.IsConnected,
            Priority = account.Priority,
            SupportsCOD = account.SupportsCOD,
            SupportsReverse = account.SupportsReverse,
            SupportsExpress = account.SupportsExpress,
            CreatedAt = account.CreatedAt
        });
    }
}
