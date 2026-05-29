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
        // Accept query-string values for pagination, search, and sorting. (Updated)
        public async Task<IActionResult> Latest(int page = 1, string? query = null, string? sort = null)
        {
            bool isAdmin = User.IsInRole("Admin");

            const int pageSize = 10;

            var pageNumber = page < 1 ? 1 : page;

            var sortOrder = string.Equals(sort, "oldest", StringComparison.OrdinalIgnoreCase) ? "oldest" : "newest";

            // Repository applies the full query pipeline so the controller stays thin.
            var reports = await _repo.GetPaginatedLatestReportsAsync(isAdmin, query, sortOrder, pageNumber, pageSize);

            // ViewModel carries both the page items and the state needed to render links.
            var vm = new LatestReportsViewModel
            {
                Reports = reports.ToList(), // Convert the selected page back to the existing list shape used by the view.
                PageIndex = reports.PageIndex,
                TotalPages = reports.TotalPages,
                HasPreviousPage = reports.HasPreviousPage,
                HasNextPage = reports.HasNextPage,
                SearchQuery = query,
                SortOrder = sortOrder,
                PageSize = pageSize
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


