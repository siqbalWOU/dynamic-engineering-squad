using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Authorization;
using InfrastructureApp.Services;

namespace InfrastructureApp.Controllers;

public class HomeController : Controller
{
    // Logger for debugging/errors
    private readonly ILogger<HomeController> _logger;

    // SCRUM-103: Repository to get reports
    private readonly IReportIssueRepository? _repo;

    // Constructor (supports logger + optional repo for tests)
    public HomeController(ILogger<HomeController> logger, IReportIssueRepository? repo = null)
    {
        _logger = logger;
        _repo = repo;
    }

    public async Task<IActionResult> Index()
    {
        // If repository isn't available, show empty data to avoid errors
        if (_repo == null)
        {
            return View(new List<ReportIssue>());
        }

        // Check if current user is Admin (defaults to false if user/context is null)
        bool isAdmin = HttpContext?.User?.IsInRole("Admin") ?? false;

        // Get latest reports (filtered by role)
        var recentReports = await _repo.GetLatestReportsAsync(isAdmin);

        // Show only top 3 on homepage
        recentReports = recentReports.Take(3).ToList();

        return View(recentReports);
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel 
        { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
        });
    }

    public IActionResult About()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult FAQ()
    {
        return View();
    }
}