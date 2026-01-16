using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Bulk;

/// <summary>
/// Command to assign multiple NDR cases to an agent.
/// </summary>
[RequirePermission("ndr.bulk")]
[RequireFeature("ndr")]
public record BulkAssignNdrCommand : IRequest<Result<BulkOperationResultDto>>, ITenantRequest
{
    public List<Guid> NdrIds { get; init; } = new();
    public Guid AssignToUserId { get; init; }
}

public class BulkAssignNdrCommandHandler : IRequestHandler<BulkAssignNdrCommand, Result<BulkOperationResultDto>>
{
    private readonly ITenantDbContext _dbContext;
    private const int MaxBatchSize = 100;

    public BulkAssignNdrCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BulkOperationResultDto>> Handle(
        BulkAssignNdrCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!request.NdrIds.Any())
        {
            return Result<BulkOperationResultDto>.Failure("No NDR IDs provided.");
        }

        if (request.NdrIds.Count > MaxBatchSize)
        {
            return Result<BulkOperationResultDto>.Failure($"Maximum {MaxBatchSize} NDR cases can be assigned at once.");
        }

        // Verify agent exists and is active
        var agent = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.AssignToUserId && u.IsActive, cancellationToken);

        if (agent == null)
        {
            return Result<BulkOperationResultDto>.Failure("Agent not found or is not active.");
        }

        var errors = new List<BulkOperationErrorDto>();
        var successfulIds = new List<Guid>();

        var ndrRecords = await _dbContext.NdrRecords
            .Where(n => request.NdrIds.Contains(n.Id))
            .ToListAsync(cancellationToken);

        var foundIds = ndrRecords.Select(n => n.Id).ToHashSet();
        var notFoundIds = request.NdrIds.Except(foundIds).ToList();

        foreach (var notFoundId in notFoundIds)
        {
            errors.Add(new BulkOperationErrorDto
            {
                ItemId = notFoundId,
                Error = "NDR case not found"
            });
        }

        foreach (var ndr in ndrRecords)
        {
            try
            {
                // Skip if already resolved
                var resolvedStatuses = new[] { NdrStatus.ClosedDelivered, NdrStatus.ClosedRTO, NdrStatus.ClosedAddressUpdated, NdrStatus.Delivered };
                if (resolvedStatuses.Contains(ndr.Status))
                {
                    errors.Add(new BulkOperationErrorDto
                    {
                        ItemId = ndr.Id,
                        Reference = ndr.AwbNumber,
                        Error = "NDR case is already resolved"
                    });
                    continue;
                }

                ndr.AssignTo(request.AssignToUserId);
                successfulIds.Add(ndr.Id);
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationErrorDto
                {
                    ItemId = ndr.Id,
                    Reference = ndr.AwbNumber,
                    Error = ex.Message
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        stopwatch.Stop();

        var result = new BulkOperationResultDto
        {
            TotalRequested = request.NdrIds.Count,
            SuccessCount = successfulIds.Count,
            FailedCount = errors.Count,
            Errors = errors,
            SuccessfulIds = successfulIds,
            Duration = stopwatch.Elapsed
        };

        return Result<BulkOperationResultDto>.Success(result);
    }
}

/// <summary>
/// Command to update status of multiple NDR cases.
/// </summary>
[RequirePermission("ndr.bulk")]
[RequireFeature("ndr")]
public record BulkUpdateNdrStatusCommand : IRequest<Result<BulkOperationResultDto>>, ITenantRequest
{
    public List<Guid> NdrIds { get; init; } = new();
    public NdrStatus NewStatus { get; init; }
    public string? Remarks { get; init; }
}

public class BulkUpdateNdrStatusCommandHandler : IRequestHandler<BulkUpdateNdrStatusCommand, Result<BulkOperationResultDto>>
{
    private readonly ITenantDbContext _dbContext;
    private const int MaxBatchSize = 100;

    public BulkUpdateNdrStatusCommandHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BulkOperationResultDto>> Handle(
        BulkUpdateNdrStatusCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!request.NdrIds.Any())
        {
            return Result<BulkOperationResultDto>.Failure("No NDR IDs provided.");
        }

        if (request.NdrIds.Count > MaxBatchSize)
        {
            return Result<BulkOperationResultDto>.Failure($"Maximum {MaxBatchSize} NDR cases can be updated at once.");
        }

        var errors = new List<BulkOperationErrorDto>();
        var successfulIds = new List<Guid>();

        var ndrRecords = await _dbContext.NdrRecords
            .Where(n => request.NdrIds.Contains(n.Id))
            .ToListAsync(cancellationToken);

        var foundIds = ndrRecords.Select(n => n.Id).ToHashSet();
        var notFoundIds = request.NdrIds.Except(foundIds).ToList();

        foreach (var notFoundId in notFoundIds)
        {
            errors.Add(new BulkOperationErrorDto
            {
                ItemId = notFoundId,
                Error = "NDR case not found"
            });
        }

        foreach (var ndr in ndrRecords)
        {
            try
            {
                // Skip if already resolved and trying to set non-resolved status
                var resolvedStatuses = new[] { NdrStatus.ClosedDelivered, NdrStatus.ClosedRTO, NdrStatus.ClosedAddressUpdated, NdrStatus.Delivered };
                if (resolvedStatuses.Contains(ndr.Status) && !resolvedStatuses.Contains(request.NewStatus))
                {
                    errors.Add(new BulkOperationErrorDto
                    {
                        ItemId = ndr.Id,
                        Reference = ndr.AwbNumber,
                        Error = "Cannot reopen a resolved NDR case"
                    });
                    continue;
                }

                ndr.Resolve(request.NewStatus, request.Remarks);
                successfulIds.Add(ndr.Id);
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationErrorDto
                {
                    ItemId = ndr.Id,
                    Reference = ndr.AwbNumber,
                    Error = ex.Message
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        stopwatch.Stop();

        var result = new BulkOperationResultDto
        {
            TotalRequested = request.NdrIds.Count,
            SuccessCount = successfulIds.Count,
            FailedCount = errors.Count,
            Errors = errors,
            SuccessfulIds = successfulIds,
            Duration = stopwatch.Elapsed
        };

        return Result<BulkOperationResultDto>.Success(result);
    }
}
