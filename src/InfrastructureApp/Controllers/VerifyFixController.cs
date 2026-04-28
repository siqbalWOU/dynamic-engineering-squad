using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Models;

namespace InfrastructureApp.Controllers
{
    public class VerifyFixController : Controller
    {
        private readonly IVerifyFixService _verifyFixService;
        private readonly UserManager<Users> _userManager;

        public VerifyFixController(IVerifyFixService verifyFixService, UserManager<Users> userManager)
        {
            _verifyFixService = verifyFixService;
            _userManager = userManager;
        }

        // POST /VerifyFix/Toggle/5
        // Returns JSON: { verifyCount, userHasVerified, threshold }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Toggle(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var (verifyCount, userHasVerified) = await _verifyFixService.ToggleVerificationAsync(id, userId);

            return Json(new
            {
                verifyCount,
                userHasVerified,
                threshold = VerifyFixService.VerificationThreshold,
                verified = verifyCount >= VerifyFixService.VerificationThreshold
            });
        }

        // GET /VerifyFix/Status/5
        [HttpGet]
        public async Task<IActionResult> Status(int id)
        {
            var userId = _userManager.GetUserId(User);
            var (verifyCount, userHasVerified) = await _verifyFixService.GetVerifyStatusAsync(id, userId);

            return Json(new
            {
                verifyCount,
                userHasVerified,
                threshold = VerifyFixService.VerificationThreshold,
                verified = verifyCount >= VerifyFixService.VerificationThreshold
            });
        }
    }
}
