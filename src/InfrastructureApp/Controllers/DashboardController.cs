using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InfrastructureApp.Controllers
{
    // Handles requests for the Dashboard page
    [Authorize]
    public class DashboardController : Controller
    {
        // Repository used to load dashboard data
        private readonly IDashboardRepository _dashboardRepo;

        // Inject repository through dependency injection
        public DashboardController(IDashboardRepository dashboardRepo)
        {
            _dashboardRepo = dashboardRepo;
        }

        // GET: /Dashboard
        // If username is provided, shows that user's dashboard; otherwise shows the logged-in user's own dashboard.
        [HttpGet]
        public async Task<IActionResult> Index(string? username = null)
        {
            DashboardViewModel vm;

            if (!string.IsNullOrWhiteSpace(username))
            {
                var profile = await _dashboardRepo.GetPublicProfileAsync(username);
                if (profile == null) return NotFound();
                vm = profile;
            }
            else
            {
                vm = await _dashboardRepo.GetDashboardSummaryAsync();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBackground(string? selectedBackgroundKey)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var updated = await _dashboardRepo.UpdateSelectedDashboardBackgroundAsync(userId, selectedBackgroundKey);
            if (updated)
            {
                TempData["Success"] = string.IsNullOrWhiteSpace(selectedBackgroundKey)
                    ? "Dashboard background reset to default."
                    : "Dashboard background updated.";
            }
            else
            {
                TempData["Error"] = "That dashboard background is unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateActivitySummaryBackground(string? selectedBackgroundKey)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var updated = await _dashboardRepo.UpdateSelectedActivitySummaryBackgroundAsync(userId, selectedBackgroundKey);
            if (updated)
            {
                TempData["Success"] = string.IsNullOrWhiteSpace(selectedBackgroundKey)
                    ? "Activity summary background reset to default."
                    : "Activity summary background updated.";
            }
            else
            {
                TempData["Error"] = "That activity summary background is unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBorder(string? selectedBorderKey)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var updated = await _dashboardRepo.UpdateSelectedDashboardBorderAsync(userId, selectedBorderKey);
            if (updated)
            {
                TempData["Success"] = string.IsNullOrWhiteSpace(selectedBorderKey)
                    ? "Dashboard border reset to default."
                    : "Dashboard border updated.";
            }
            else
            {
                TempData["Error"] = "That dashboard border is unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateActivitySummaryBorder(string? selectedBorderKey)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var updated = await _dashboardRepo.UpdateSelectedActivitySummaryBorderAsync(userId, selectedBorderKey);
            if (updated)
            {
                TempData["Success"] = string.IsNullOrWhiteSpace(selectedBorderKey)
                    ? "Activity summary border reset to default."
                    : "Activity summary border updated.";
            }
            else
            {
                TempData["Error"] = "That activity summary border is unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
