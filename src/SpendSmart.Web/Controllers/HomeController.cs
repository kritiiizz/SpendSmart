using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Reports;
using SpendSmart.Web.ViewModels;

namespace SpendSmart.Web.Controllers;

public sealed class HomeController(IReportService reports) : AppController
{
    [AllowAnonymous]
    public async Task<IActionResult> Index(int? year, int? month, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View("Landing");
        }

        var dashboard = await reports.GetDashboardAsync(year, month, cancellationToken);
        return View("Dashboard", new DashboardViewModel(dashboard));
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // UseExceptionHandler's StatusCodeSelector has already set 404 or 500.
        var status = Response.StatusCode >= 400 ? Response.StatusCode : 500;
        return View("Error", new ErrorViewModel(status, Activity.Current?.Id ?? HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [Route("home/status/{code:int}")]
    public IActionResult Status(int code) =>
        View("Error", new ErrorViewModel(code, Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}
