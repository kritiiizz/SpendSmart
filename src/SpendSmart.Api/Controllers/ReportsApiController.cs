using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Reports;

namespace SpendSmart.Api.Controllers;

[Route("api/reports")]
[Tags("Reports")]
public sealed class ReportsApiController(IReportService reports) : ApiControllerBase
{
    /// <summary>Totals by category, a six-month trend, month-over-month change and budget alerts. Cached per user.</summary>
    [HttpGet("dashboard")]
    [EndpointSummary("Monthly dashboard summary (defaults to the current month)")]
    [ProducesResponseType<DashboardDto>(StatusCodes.Status200OK)]
    public Task<DashboardDto> Dashboard([FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken) =>
        reports.GetDashboardAsync(year, month, cancellationToken);
}
