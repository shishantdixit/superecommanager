using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Subscriptions;

namespace SuperEcomManager.Application.Features.PlatformAdmin;

#region DTOs

public class PlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; } = "INR";
    public int MaxUsers { get; set; }
    public int MaxOrders { get; set; }
    public int MaxChannels { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<string> Features { get; set; } = new();
    public int SubscriberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FeatureDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCore { get; set; }
}

#endregion

#region Commands

/// <summary>
/// Command to create a new plan.
/// </summary>
public class CreatePlanCommand : IRequest<Result<PlanDto>>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxOrders { get; set; }
    public int MaxChannels { get; set; }
    public int SortOrder { get; set; }
    public List<Guid> FeatureIds { get; set; } = new();
}

public class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, Result<PlanDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CreatePlanCommandHandler> _logger;

    public CreatePlanCommandHandler(IApplicationDbContext dbContext, ILogger<CreatePlanCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<PlanDto>> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        // Validate code uniqueness
        var codeExists = await _dbContext.Plans
            .AnyAsync(p => p.Code == request.Code.ToLowerInvariant(), cancellationToken);

        if (codeExists)
        {
            return Result<PlanDto>.Failure($"Plan with code '{request.Code}' already exists");
        }

        var plan = Plan.Create(
            request.Name,
            request.Code,
            request.MonthlyPrice,
            request.YearlyPrice,
            request.MaxUsers,
            request.MaxOrders,
            request.MaxChannels);

        plan.SetSortOrder(request.SortOrder);

        _dbContext.Plans.Add(plan);

        // Add features
        if (request.FeatureIds.Any())
        {
            var features = await _dbContext.Features
                .Where(f => request.FeatureIds.Contains(f.Id))
                .ToListAsync(cancellationToken);

            foreach (var feature in features)
            {
                _dbContext.PlanFeatures.Add(new PlanFeature(plan.Id, feature.Id));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created plan {PlanCode}", plan.Code);

        return Result<PlanDto>.Success(await GetPlanDtoAsync(plan.Id, cancellationToken));
    }

    private async Task<PlanDto> GetPlanDtoAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.Plans
            .AsNoTracking()
            .FirstAsync(p => p.Id == planId, cancellationToken);

        var features = await _dbContext.PlanFeatures
            .AsNoTracking()
            .Include(pf => pf.Feature)
            .Where(pf => pf.PlanId == planId)
            .Select(pf => pf.Feature!.Name)
            .ToListAsync(cancellationToken);

        return new PlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Code = plan.Code,
            Description = plan.Description,
            MonthlyPrice = plan.MonthlyPrice,
            YearlyPrice = plan.YearlyPrice,
            Currency = plan.Currency,
            MaxUsers = plan.MaxUsers,
            MaxOrders = plan.MaxOrders,
            MaxChannels = plan.MaxChannels,
            IsActive = plan.IsActive,
            SortOrder = plan.SortOrder,
            Features = features,
            CreatedAt = plan.CreatedAt
        };
    }
}

/// <summary>
/// Command to update a plan.
/// </summary>
public class UpdatePlanCommand : IRequest<Result<PlanDto>>
{
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxOrders { get; set; }
    public int MaxChannels { get; set; }
    public int SortOrder { get; set; }
    public List<Guid> FeatureIds { get; set; } = new();
}

public class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, Result<PlanDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<UpdatePlanCommandHandler> _logger;

    public UpdatePlanCommandHandler(IApplicationDbContext dbContext, ILogger<UpdatePlanCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<PlanDto>> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.Plans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

        if (plan == null)
        {
            return Result<PlanDto>.Failure("Plan not found");
        }

        plan.Update(
            request.Name,
            request.Description,
            request.MonthlyPrice,
            request.YearlyPrice,
            request.MaxUsers,
            request.MaxOrders,
            request.MaxChannels,
            request.SortOrder);

        // Update features - remove existing and add new
        var existingFeatures = await _dbContext.PlanFeatures
            .Where(pf => pf.PlanId == plan.Id)
            .ToListAsync(cancellationToken);

        _dbContext.PlanFeatures.RemoveRange(existingFeatures);

        foreach (var featureId in request.FeatureIds)
        {
            _dbContext.PlanFeatures.Add(new PlanFeature(plan.Id, featureId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated plan {PlanCode}", plan.Code);

        var features = await _dbContext.PlanFeatures
            .AsNoTracking()
            .Include(pf => pf.Feature)
            .Where(pf => pf.PlanId == plan.Id)
            .Select(pf => pf.Feature!.Name)
            .ToListAsync(cancellationToken);

        return Result<PlanDto>.Success(new PlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Code = plan.Code,
            Description = plan.Description,
            MonthlyPrice = plan.MonthlyPrice,
            YearlyPrice = plan.YearlyPrice,
            Currency = plan.Currency,
            MaxUsers = plan.MaxUsers,
            MaxOrders = plan.MaxOrders,
            MaxChannels = plan.MaxChannels,
            IsActive = plan.IsActive,
            SortOrder = plan.SortOrder,
            Features = features,
            CreatedAt = plan.CreatedAt
        });
    }
}

/// <summary>
/// Command to activate a plan.
/// </summary>
public class ActivatePlanCommand : IRequest<Result>
{
    public Guid PlanId { get; set; }
}

public class ActivatePlanCommandHandler : IRequestHandler<ActivatePlanCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ActivatePlanCommandHandler> _logger;

    public ActivatePlanCommandHandler(IApplicationDbContext dbContext, ILogger<ActivatePlanCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(ActivatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.Plans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

        if (plan == null)
        {
            return Result.Failure("Plan not found");
        }

        plan.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated plan {PlanCode}", plan.Code);

        return Result.Success();
    }
}

/// <summary>
/// Command to deactivate a plan.
/// </summary>
public class DeactivatePlanCommand : IRequest<Result>
{
    public Guid PlanId { get; set; }
}

public class DeactivatePlanCommandHandler : IRequestHandler<DeactivatePlanCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<DeactivatePlanCommandHandler> _logger;

    public DeactivatePlanCommandHandler(IApplicationDbContext dbContext, ILogger<DeactivatePlanCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(DeactivatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.Plans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

        if (plan == null)
        {
            return Result.Failure("Plan not found");
        }

        plan.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated plan {PlanCode}", plan.Code);

        return Result.Success();
    }
}

#endregion

#region Queries

/// <summary>
/// Query to get all plans.
/// </summary>
public class GetPlansQuery : IRequest<List<PlanDto>>
{
    public bool? IsActive { get; set; }
    public bool IncludeSubscriberCount { get; set; }
}

public class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, List<PlanDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPlansQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<PlanDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Plans.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        var plans = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.MonthlyPrice)
            .ToListAsync(cancellationToken);

        var planIds = plans.Select(p => p.Id).ToList();

        var features = await _dbContext.PlanFeatures
            .AsNoTracking()
            .Include(pf => pf.Feature)
            .Where(pf => planIds.Contains(pf.PlanId))
            .ToListAsync(cancellationToken);

        var featuresByPlan = features
            .GroupBy(pf => pf.PlanId)
            .ToDictionary(g => g.Key, g => g.Select(pf => pf.Feature!.Name).ToList());

        Dictionary<Guid, int> subscriberCounts = new();
        if (request.IncludeSubscriberCount)
        {
            var activeStatuses = new[] { Domain.Enums.SubscriptionStatus.Active, Domain.Enums.SubscriptionStatus.Trial };
            subscriberCounts = await _dbContext.Subscriptions
                .AsNoTracking()
                .Where(s => activeStatuses.Contains(s.Status))
                .GroupBy(s => s.PlanId)
                .Select(g => new { PlanId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PlanId, x => x.Count, cancellationToken);
        }

        return plans.Select(p => new PlanDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Description = p.Description,
            MonthlyPrice = p.MonthlyPrice,
            YearlyPrice = p.YearlyPrice,
            Currency = p.Currency,
            MaxUsers = p.MaxUsers,
            MaxOrders = p.MaxOrders,
            MaxChannels = p.MaxChannels,
            IsActive = p.IsActive,
            SortOrder = p.SortOrder,
            Features = featuresByPlan.GetValueOrDefault(p.Id) ?? new List<string>(),
            SubscriberCount = subscriberCounts.GetValueOrDefault(p.Id),
            CreatedAt = p.CreatedAt
        }).ToList();
    }
}

/// <summary>
/// Query to get a plan by ID.
/// </summary>
public class GetPlanByIdQuery : IRequest<Result<PlanDto>>
{
    public Guid PlanId { get; set; }
}

public class GetPlanByIdQueryHandler : IRequestHandler<GetPlanByIdQuery, Result<PlanDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPlanByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PlanDto>> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

        if (plan == null)
        {
            return Result<PlanDto>.Failure("Plan not found");
        }

        var features = await _dbContext.PlanFeatures
            .AsNoTracking()
            .Include(pf => pf.Feature)
            .Where(pf => pf.PlanId == plan.Id)
            .Select(pf => pf.Feature!.Name)
            .ToListAsync(cancellationToken);

        var activeStatuses = new[] { Domain.Enums.SubscriptionStatus.Active, Domain.Enums.SubscriptionStatus.Trial };
        var subscriberCount = await _dbContext.Subscriptions
            .CountAsync(s => s.PlanId == plan.Id && activeStatuses.Contains(s.Status), cancellationToken);

        return Result<PlanDto>.Success(new PlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Code = plan.Code,
            Description = plan.Description,
            MonthlyPrice = plan.MonthlyPrice,
            YearlyPrice = plan.YearlyPrice,
            Currency = plan.Currency,
            MaxUsers = plan.MaxUsers,
            MaxOrders = plan.MaxOrders,
            MaxChannels = plan.MaxChannels,
            IsActive = plan.IsActive,
            SortOrder = plan.SortOrder,
            Features = features,
            SubscriberCount = subscriberCount,
            CreatedAt = plan.CreatedAt
        });
    }
}

/// <summary>
/// Query to get all features.
/// </summary>
public class GetFeaturesQuery : IRequest<List<FeatureDto>>
{
}

public class GetFeaturesQueryHandler : IRequestHandler<GetFeaturesQuery, List<FeatureDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetFeaturesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<FeatureDto>> Handle(GetFeaturesQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Features
            .AsNoTracking()
            .OrderBy(f => f.Module)
            .ThenBy(f => f.Name)
            .Select(f => new FeatureDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                Module = f.Module,
                Description = f.Description,
                IsCore = f.IsCore
            })
            .ToListAsync(cancellationToken);
    }
}

#endregion
