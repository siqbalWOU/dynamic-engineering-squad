using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    // Controller handles requests for Reports pages
    public class ReportsController : Controller
    {
        private readonly IReportIssueRepository _repo;
        private readonly IVerifyFixService _verifyFixService;

        public ReportsController(IReportIssueRepository repo, IVerifyFixService verifyFixService)
        {
            _repo = repo;
            _verifyFixService = verifyFixService;
        }

        // GET: /Reports/Latest
        [HttpGet]
        public async Task<IActionResult> Latest()
        {
            bool isAdmin = User.IsInRole("Admin");

            // Repository handles filtering and sorting
            var reports = await _repo.GetLatestReportsAsync(isAdmin);

            // Pass data directly to ViewModel (no copying)
            var vm = new LatestReportsViewModel
            {
                Reports = reports
            };

            return View(vm);
        }

        // GET: /Reports/Verify
        [HttpGet]
        public async Task<IActionResult> Verify()
        {
            var reports = await _repo.GetResolvedReportsAsync();
            var counts = await _verifyFixService.GetVerifyCountsAsync(reports.Select(r => r.Id));

            var vm = new VerifyFixViewModel
            {
                Reports = reports.Select(r => new VerifyFixItemViewModel
                {
                    Report = r,
                    VerifyCount = counts.GetValueOrDefault(r.Id, 0)
                }).ToList()
            };

            return View(vm);
        }
    }
}


