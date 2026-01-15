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
/// Command to add a remark/note to an NDR case.
/// </summary>
[RequirePermission("ndr.edit")]
[RequireFeature("ndr_management")]
public record AddNdrRemarkCommand : IRequest<Result<NdrRemarkDto>>, ITenantRequest
{
    public Guid NdrRecordId { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsInternal { get; init; } = true;
}

public class AddNdrRemarkCommandHandler : IRequestHandler<AddNdrRemarkCommand, Result<NdrRemarkDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AddNdrRemarkCommandHandler> _logger;

    public AddNdrRemarkCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<AddNdrRemarkCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<NdrRemarkDto>> Handle(
        AddNdrRemarkCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Result<NdrRemarkDto>.Failure("Remark content is required");
        }

        var ndr = await _dbContext.NdrRecords
            .FirstOrDefaultAsync(n => n.Id == request.NdrRecordId, cancellationToken);

        if (ndr == null)
        {
            return Result<NdrRemarkDto>.Failure("NDR case not found");
        }

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        // Create remark
        var remark = new NdrRemark(
            ndr.Id,
            currentUserId,
            request.Content,
            request.IsInternal);

        ndr.AddRemark(remark);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Remark added to NDR case {NdrRecordId} by user {UserId}",
            ndr.Id, currentUserId);

        // Get user name for response
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        var dto = new NdrRemarkDto
        {
            Id = remark.Id,
            UserId = remark.UserId,
            UserName = user?.FullName ?? "Unknown",
            Content = remark.Content,
            CreatedAt = remark.CreatedAt,
            IsInternal = remark.IsInternal
        };

        return Result<NdrRemarkDto>.Success(dto);
    }
}
