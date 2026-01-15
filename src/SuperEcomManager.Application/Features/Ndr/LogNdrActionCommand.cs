using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.NDR;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Ndr;

/// <summary>
/// Command to log an action (call, WhatsApp, SMS, Email) for an NDR case.
/// </summary>
[RequirePermission("ndr.edit")]
[RequireFeature("ndr_management")]
public record LogNdrActionCommand : IRequest<Result<NdrActionDto>>, ITenantRequest
{
    public Guid NdrRecordId { get; init; }
    public NdrActionType ActionType { get; init; }
    public string? Details { get; init; }
    public string? Outcome { get; init; }
    public int? CallDurationSeconds { get; init; }
}

public class LogNdrActionCommandHandler : IRequestHandler<LogNdrActionCommand, Result<NdrActionDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LogNdrActionCommandHandler> _logger;

    public LogNdrActionCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<LogNdrActionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<NdrActionDto>> Handle(
        LogNdrActionCommand request,
        CancellationToken cancellationToken)
    {
        var ndr = await _dbContext.NdrRecords
            .FirstOrDefaultAsync(n => n.Id == request.NdrRecordId, cancellationToken);

        if (ndr == null)
        {
            return Result<NdrActionDto>.Failure("NDR case not found");
        }

        // Check if case is already resolved
        if (ndr.Status == NdrStatus.ClosedDelivered ||
            ndr.Status == NdrStatus.ClosedRTO ||
            ndr.Status == NdrStatus.ClosedAddressUpdated)
        {
            return Result<NdrActionDto>.Failure("Cannot log action on a closed NDR case");
        }

        // Validate action type for call logging
        var validActionTypes = new[]
        {
            NdrActionType.PhoneCall,
            NdrActionType.WhatsAppMessage,
            NdrActionType.SMS,
            NdrActionType.Email,
            NdrActionType.RemarkAdded,
            NdrActionType.CallbackScheduled
        };

        if (!validActionTypes.Contains(request.ActionType))
        {
            return Result<NdrActionDto>.Failure($"Invalid action type: {request.ActionType}. Use specific commands for reattempt, RTO, or escalation.");
        }

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        // Create action
        var action = NdrAction.Create(
            ndr.Id,
            request.ActionType,
            currentUserId,
            request.Details,
            request.Outcome,
            request.CallDurationSeconds);

        ndr.AddAction(action);

        // Update status if customer was contacted
        if (ndr.Status == NdrStatus.Assigned &&
            (request.ActionType == NdrActionType.PhoneCall ||
             request.ActionType == NdrActionType.WhatsAppMessage ||
             request.ActionType == NdrActionType.SMS ||
             request.ActionType == NdrActionType.Email))
        {
            // Status progression can be handled here
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "NDR action logged for case {NdrRecordId}: {ActionType} by user {UserId}",
            ndr.Id, request.ActionType, _currentUserService.UserId);

        // Get user name for response
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        var dto = new NdrActionDto
        {
            Id = action.Id,
            ActionType = action.ActionType,
            PerformedByUserId = action.PerformedByUserId,
            PerformedByUserName = user?.FullName ?? "Unknown",
            PerformedAt = action.PerformedAt,
            Details = action.Details,
            Outcome = action.Outcome,
            CallDurationSeconds = action.CallDurationSeconds
        };

        return Result<NdrActionDto>.Success(dto);
    }
}
