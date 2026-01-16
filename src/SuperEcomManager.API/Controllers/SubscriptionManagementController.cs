using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Features.PlatformAdmin;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for subscription and plan management (Platform Admin).
/// </summary>
[ApiController]
[Route("api/platform-admin")]
[Authorize(Policy = "PlatformAdmin")]
public class SubscriptionManagementController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionManagementController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region Plans

    /// <summary>
    /// Get all plans.
    /// </summary>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<PlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(
        [FromQuery] bool? isActive,
        [FromQuery] bool includeSubscriberCount = false)
    {
        var result = await _mediator.Send(new GetPlansQuery
        {
            IsActive = isActive,
            IncludeSubscriberCount = includeSubscriberCount
        });

        return Ok(result);
    }

    /// <summary>
    /// Get plan by ID.
    /// </summary>
    [HttpGet("plans/{planId:guid}")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlan(Guid planId)
    {
        var result = await _mediator.Send(new GetPlanByIdQuery { PlanId = planId });

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new plan.
    /// </summary>
    [HttpPost("plans")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
    {
        var result = await _mediator.Send(new CreatePlanCommand
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            MonthlyPrice = request.MonthlyPrice,
            YearlyPrice = request.YearlyPrice,
            MaxUsers = request.MaxUsers,
            MaxOrders = request.MaxOrders,
            MaxChannels = request.MaxChannels,
            SortOrder = request.SortOrder,
            FeatureIds = request.FeatureIds
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return CreatedAtAction(
            nameof(GetPlan),
            new { planId = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// Update a plan.
    /// </summary>
    [HttpPut("plans/{planId:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdatePlanRequest request)
    {
        var result = await _mediator.Send(new UpdatePlanCommand
        {
            PlanId = planId,
            Name = request.Name,
            Description = request.Description,
            MonthlyPrice = request.MonthlyPrice,
            YearlyPrice = request.YearlyPrice,
            MaxUsers = request.MaxUsers,
            MaxOrders = request.MaxOrders,
            MaxChannels = request.MaxChannels,
            SortOrder = request.SortOrder,
            FeatureIds = request.FeatureIds
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Activate a plan.
    /// </summary>
    [HttpPost("plans/{planId:guid}/activate")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivatePlan(Guid planId)
    {
        var result = await _mediator.Send(new ActivatePlanCommand { PlanId = planId });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Plan activated successfully" });
    }

    /// <summary>
    /// Deactivate a plan.
    /// </summary>
    [HttpPost("plans/{planId:guid}/deactivate")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivatePlan(Guid planId)
    {
        var result = await _mediator.Send(new DeactivatePlanCommand { PlanId = planId });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Plan deactivated successfully" });
    }

    /// <summary>
    /// Get all features.
    /// </summary>
    [HttpGet("features")]
    [ProducesResponseType(typeof(List<FeatureDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatures()
    {
        var result = await _mediator.Send(new GetFeaturesQuery());
        return Ok(result);
    }

    #endregion

    #region Subscriptions

    /// <summary>
    /// Get subscriptions with filtering.
    /// </summary>
    [HttpGet("subscriptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? planId,
        [FromQuery] SubscriptionStatus? status,
        [FromQuery] bool? isExpiringSoon,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetSubscriptionsQuery
        {
            TenantId = tenantId,
            PlanId = planId,
            Status = status,
            IsExpiringSoon = isExpiringSoon,
            Page = page,
            PageSize = Math.Min(pageSize, 100)
        });

        return Ok(result);
    }

    /// <summary>
    /// Get subscription by ID.
    /// </summary>
    [HttpGet("subscriptions/{subscriptionId:guid}")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription(Guid subscriptionId)
    {
        var result = await _mediator.Send(new GetSubscriptionByIdQuery { SubscriptionId = subscriptionId });

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Change tenant's subscription plan.
    /// </summary>
    [HttpPost("tenants/{tenantId:guid}/change-plan")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeTenantPlan(Guid tenantId, [FromBody] ChangePlanRequest request)
    {
        var result = await _mediator.Send(new ChangeTenantPlanCommand
        {
            TenantId = tenantId,
            NewPlanId = request.PlanId,
            IsYearly = request.IsYearly,
            Notes = request.Notes
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Activate a subscription (convert trial to paid).
    /// </summary>
    [HttpPost("subscriptions/{subscriptionId:guid}/activate")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateSubscription(Guid subscriptionId, [FromBody] ActivateSubscriptionRequest request)
    {
        var result = await _mediator.Send(new ActivateSubscriptionCommand
        {
            SubscriptionId = subscriptionId,
            IsYearly = request.IsYearly,
            OverridePrice = request.OverridePrice
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cancel a subscription.
    /// </summary>
    [HttpPost("subscriptions/{subscriptionId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelSubscription(Guid subscriptionId, [FromBody] CancelSubscriptionRequest request)
    {
        var result = await _mediator.Send(new CancelSubscriptionCommand
        {
            SubscriptionId = subscriptionId,
            Reason = request.Reason,
            ImmediateCancel = request.ImmediateCancel
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Subscription cancelled successfully" });
    }

    /// <summary>
    /// Renew a subscription.
    /// </summary>
    [HttpPost("subscriptions/{subscriptionId:guid}/renew")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenewSubscription(Guid subscriptionId, [FromBody] RenewSubscriptionRequest request)
    {
        var result = await _mediator.Send(new RenewSubscriptionCommand
        {
            SubscriptionId = subscriptionId,
            IsYearly = request.IsYearly,
            OverridePrice = request.OverridePrice
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    #endregion
}

#region Request DTOs

public record CreatePlanRequest(
    string Name,
    string Code,
    string? Description,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int MaxUsers,
    int MaxOrders,
    int MaxChannels,
    int SortOrder = 0,
    List<Guid>? FeatureIds = null)
{
    public List<Guid> FeatureIds { get; init; } = FeatureIds ?? new();
}

public record UpdatePlanRequest(
    string Name,
    string? Description,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int MaxUsers,
    int MaxOrders,
    int MaxChannels,
    int SortOrder = 0,
    List<Guid>? FeatureIds = null)
{
    public List<Guid> FeatureIds { get; init; } = FeatureIds ?? new();
}

public record ChangePlanRequest(Guid PlanId, bool IsYearly, string? Notes = null);
public record ActivateSubscriptionRequest(bool IsYearly, decimal? OverridePrice = null);
public record CancelSubscriptionRequest(string? Reason = null, bool ImmediateCancel = false);
public record RenewSubscriptionRequest(bool IsYearly, decimal? OverridePrice = null);

#endregion
