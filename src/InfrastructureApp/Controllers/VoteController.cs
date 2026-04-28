using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Models;

namespace InfrastructureApp.Controllers
{
    public class VoteController : Controller
    {
        private readonly IVoteService _voteService;
        private readonly UserManager<Users> _userManager;

        public VoteController(IVoteService voteService, UserManager<Users> userManager)
        {
            _voteService = voteService;
            _userManager = userManager;
        }

        // POST /Vote/Toggle/5
        // Returns JSON: { voteCount, userHasVoted }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Toggle(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var (voteCount, userHasVoted) = await _voteService.ToggleVoteAsync(id, userId);

            return Json(new { voteCount, userHasVoted });
        }

        // GET /Vote/Status/5
        // Returns current vote count and whether the current user has voted.
        [HttpGet]
        public async Task<IActionResult> Status(int id)
        {
            var userId = _userManager.GetUserId(User);
            var (voteCount, userHasVoted) = await _voteService.GetVoteStatusAsync(id, userId);

            return Json(new { voteCount, userHasVoted });
        }
    }
}
