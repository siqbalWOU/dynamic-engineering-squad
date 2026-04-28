using System.Threading.Tasks;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    [Authorize]
    public class FlagController : Controller
    {
        private readonly IFlagService _flagService;
        private readonly UserManager<Users> _userManager;

        public FlagController(IFlagService flagService, UserManager<Users> userManager)
        {
            _flagService = flagService;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Create(int reportId, string category)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not found." });
            }

            var (success, message) = await _flagService.FlagReportAsync(reportId, userId, category);
            return Json(new { success, message });
        }

        [HttpGet]
        public async Task<IActionResult> Status(int reportId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { hasUserFlagged = false });
            }

            var hasUserFlagged = await _flagService.HasUserFlaggedAsync(reportId, userId);
            return Json(new { hasUserFlagged });
        }
    }
}
