using System.Threading.Tasks;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    [Authorize(Roles = "Moderator,Admin")]
    public class ModerationController : Controller
    {
        private readonly IModerationService _moderationService;
        private readonly UserManager<Users> _userManager;

        public ModerationController(IModerationService moderationService, UserManager<Users> userManager)
        {
            _moderationService = moderationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = await _moderationService.GetDashboardViewModelAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(int reportId)
        {
            var moderatorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(moderatorId)) return Challenge();

            var (success, message) = await _moderationService.DismissReportAsync(reportId, moderatorId);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int reportId)
        {
            var moderatorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(moderatorId)) return Challenge();

            var (success, message) = await _moderationService.RemovePostAsync(reportId, moderatorId);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
