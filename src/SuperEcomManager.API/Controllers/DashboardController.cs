using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Dashboard;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Dashboard and analytics endpoints.
/// </summary>
[Authorize]
public class DashboardController : ApiControllerBase
{
    /// <summary>
    /// Get main dashboard overview with aggregated metrics.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<DashboardOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetOverview(
        [FromQuery] int periodDays = 30,
        [FromQuery] int trendDays = 14)
    {
        var result = await Mediator.Send(new GetDashboardOverviewQuery
        {
            PeriodDays = periodDays,
            TrendDays = trendDays
        });

        if (result.IsFailure)
            return BadRequestResponse<DashboardOverviewDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get orders-focused dashboard metrics.
    /// </summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiResponse<OrdersDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrdersDashboardDto>>> GetOrdersDashboard(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int topProductsCount = 10)
    {
        var result = await Mediator.Send(new GetOrdersDashboardQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            TopProductsCount = topProductsCount
        });

        if (result.IsFailure)
            return BadRequestResponse<OrdersDashboardDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get shipments-focused dashboard metrics.
    /// </summary>
    [HttpGet("shipments")]
    [ProducesResponseType(typeof(ApiResponse<ShipmentsDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ShipmentsDashboardDto>>> GetShipmentsDashboard(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await Mediator.Send(new GetShipmentsDashboardQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        });

        if (result.IsFailure)
            return BadRequestResponse<ShipmentsDashboardDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Get dashboard alerts and action items.
    /// </summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(ApiResponse<DashboardAlertsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardAlertsDto>>> GetAlerts()
    {
        var result = await Mediator.Send(new GetDashboardAlertsQuery());

        if (result.IsFailure)
            return BadRequestResponse<DashboardAlertsDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value!);
    }
}
