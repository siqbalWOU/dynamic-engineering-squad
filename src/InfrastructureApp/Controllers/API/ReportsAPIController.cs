using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.API
{
    // API controller used by the Latest Reports page.
    // Supports AJAX features like search, sort, and modal detail loading.
    [Route("api/reports")]
    [ApiController]
    public class ReportsAPIController : ControllerBase
    {
        // Repo gives access to ReportIssue data from the database
        private readonly IReportIssueRepository _repo;
        private readonly ILogger<ReportsAPIController> _logger;

        // Constructor with dependency injection
        // Repository handles data access, logger records errors if something fails.
        public ReportsAPIController(IReportIssueRepository repo, ILogger<ReportsAPIController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // GET: /api/reports/latest?query=keyword&sort=newest
        // Called by JavaScript search/sort on the Latest Reports page
        [HttpGet("latest")]
        public async Task<ActionResult<IEnumerable<object>>> Latest([FromQuery] string? query, [FromQuery] string? sort) // SCRUM-86 UPDATED: added sort parameter
        {
            // Check if current user is Admin (affects which reports are visible)
            bool isAdmin = User.IsInRole("Admin");

            try
            {
                // Get filtered reports from repository
                // Repository applies visibility rules, keyword filtering, and sorting
                var reports = await _repo.SearchLatestReportsAsync(isAdmin, query, sort);

                // Convert reports into a lightweight shape for the list display.
                // The list view does NOT need coordinates, so they are excluded here.
                var reportResults = reports.Select(r => new
                {
                    r.Id,           // Used to identify which report was clicked
                    r.Description,  // Main text shown in the list
                    r.Status,       // Shows if report is Approved, Pending, etc.
                    r.ImageUrl,     // Optional preview image
                    r.CreatedAt     // JavaScript will format this date
                });

                // Return results as JSON
                return Ok(reportResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching latest reports");

                // Return empty list so UI does not crash
                return Ok(Array.Empty<object>());
            }
        }

        // GET: /api/reports/5
        // SCRUM-98: returns one report's details for the popup modal, including map coordinates
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetReportById([FromRoute] int id)
        {   
            // Admin users can see all reports; non-admins only see approved ones
            bool isAdmin = User.IsInRole("Admin");

            try
            {
                // Retrieve the selected report by its Id for the modal details
                var report = await _repo.GetByIdAsync(id);
                
                 // If report does not exist return 404
                if (report == null)
                {
                    return NotFound();
                }

                // Non-admin users should not see non-approved reports
                if (!isAdmin && report.Status != "Approved")
                {
                    return NotFound();
                }

                // Return the full details needed by the popup modal
                // These fields appear again because the modal loads the selected report separately from /api/reports/{id}.
                // Latitude and Longitude are included so the map script can display the report location.
                return Ok(new
                {
                    report.Id,
                    report.Description,
                    report.Status,
                    report.ImageUrl,
                    report.CreatedAt,
                    report.Latitude, // used by latestReportsMap.js for the map marker
                    report.Longitude // used by latestReportsMap.js for the map marker
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report details for report id {ReportId}", id);

                // Return NotFound to avoid exposing internal errors
                return NotFound();
            }
        }
    }
}