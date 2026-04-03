using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        // GET: /Dashboard/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Get dashboard summary data from repository
            var vm = await _dashboardRepo.GetDashboardSummaryAsync();

            // Pass ViewModel to Razor view
            return View(vm);
        }
    }
}