using Keues.API.Responses;
using Keues.Application.Features.Dashboard;
using Keues.Application.Features.Dashboard.GetDashboardSummary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Keues.API.Controllers
{
  /// <summary>
  /// Dashboard summary for a location.
  /// </summary>
  [Route("api/[controller]")]
  [ApiController]
  public class DashboardController(DashboardUseCases useCases) : ControllerBase
  {
    /// <summary>
    /// Gets the dashboard summary for a location (KPIs, in progress, waiting by queue, and activity per hour).
    /// </summary>
    /// <param name="locationId">Identifier of the location.</param>
    /// <param name="date">Optional date of the day to query. By default, the current day in UTC.</param>
    /// <returns>Dashboard summary.</returns>
    /// <response code="200">Summary obtained.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetDashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] Guid locationId, [FromQuery] DateTime? date)
    {
      try
      {
        var summary = await useCases.Summary.Handle(new GetDashboardSummaryCommand
        {
          LocationId = locationId,
          Date = date
        });
        return Ok(summary);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
  }
}