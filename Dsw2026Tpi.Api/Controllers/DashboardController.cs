using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/dashboard")]
[Authorize(Policy = Policies.AdminPolicy)]
public class DashboardController : AppController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(
        IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    [ProducesResponseType(
        typeof(DashboardModel.StatsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateTime? date)
    {
        var stats =
            await _dashboardService.GetStatsAsync(
                date);

        return Ok(stats);
    }
}
