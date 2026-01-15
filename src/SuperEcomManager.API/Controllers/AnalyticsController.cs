using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Analytics;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Advanced analytics and trend analysis endpoints.
/// </summary>
[Authorize]
public class AnalyticsController : ApiControllerBase
{
    /// <summary>
    /// Get revenue trends with period comparison.
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(ApiResponse<RevenueTrendsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RevenueTrendsDto>>> GetRevenueTrends(
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Last30Days,
        [FromQuery] DateTime? customStartDate = null,
        [FromQuery] DateTime? customEndDate = null)
    {
        var result = await Mediator.Send(new GetRevenueTrendsQuery
        {
            Period = period,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate
        });

        if (result.IsFailure)
            return BadRequestResponse<RevenueTrendsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get order trends with status breakdown.
    /// </summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiResponse<OrderTrendsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrderTrendsDto>>> GetOrderTrends(
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Last30Days,
        [FromQuery] DateTime? customStartDate = null,
        [FromQuery] DateTime? customEndDate = null)
    {
        var result = await Mediator.Send(new GetOrderTrendsQuery
        {
            Period = period,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate
        });

        if (result.IsFailure)
            return BadRequestResponse<OrderTrendsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get delivery performance metrics.
    /// </summary>
    [HttpGet("delivery")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DeliveryPerformanceDto>>> GetDeliveryPerformance(
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Last30Days,
        [FromQuery] DateTime? customStartDate = null,
        [FromQuery] DateTime? customEndDate = null)
    {
        var result = await Mediator.Send(new GetDeliveryPerformanceQuery
        {
            Period = period,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate
        });

        if (result.IsFailure)
            return BadRequestResponse<DeliveryPerformanceDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get courier comparison analytics.
    /// </summary>
    [HttpGet("couriers")]
    [ProducesResponseType(typeof(ApiResponse<CourierComparisonDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CourierComparisonDto>>> GetCourierComparison(
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Last30Days,
        [FromQuery] DateTime? customStartDate = null,
        [FromQuery] DateTime? customEndDate = null)
    {
        var result = await Mediator.Send(new GetCourierComparisonQuery
        {
            Period = period,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate
        });

        if (result.IsFailure)
            return BadRequestResponse<CourierComparisonDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get NDR analytics with resolution rates and agent performance.
    /// </summary>
    [HttpGet("ndr")]
    [ProducesResponseType(typeof(ApiResponse<NdrAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<NdrAnalyticsDto>>> GetNdrAnalytics(
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.Last30Days,
        [FromQuery] DateTime? customStartDate = null,
        [FromQuery] DateTime? customEndDate = null)
    {
        var result = await Mediator.Send(new GetNdrAnalyticsQuery
        {
            Period = period,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate
        });

        if (result.IsFailure)
            return BadRequestResponse<NdrAnalyticsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }
}
