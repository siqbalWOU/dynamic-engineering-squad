using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.Services.Minigames;
using InfrastructureApp.ViewModels.Minigames;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    [Authorize]
    public class MinigamesController : Controller
    {
        private readonly IMinigameService _minigameService;
        private readonly IMinigameViewModelFactory _minigameViewModelFactory;
        private readonly UserManager<Users> _userManager;
        private readonly IAuditLogService _auditLogService;

        public MinigamesController(
            IMinigameService minigameService,
            IMinigameViewModelFactory minigameViewModelFactory,
            UserManager<Users> userManager,
            IAuditLogService auditLogService)
        {
            _minigameService = minigameService;
            _minigameViewModelFactory = minigameViewModelFactory;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            return View(await _minigameViewModelFactory.CreateIndexViewModelAsync(userId));
        }

        [HttpGet]
        public async Task<IActionResult> Slots()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            return View(await _minigameViewModelFactory.CreateSlotsViewModelAsync(userId));
        }

        [HttpGet]
        public async Task<IActionResult> Matching()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            return View(await _minigameViewModelFactory.CreateMatchingViewModelAsync(userId));
        }

        [HttpGet]
        public async Task<IActionResult> Trivia()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            return View(await _minigameViewModelFactory.CreateTriviaViewModelAsync(userId));
        }

        [HttpGet]
        public async Task<IActionResult> TapRepair()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            return View(await _minigameViewModelFactory.CreateTapRepairViewModelAsync(userId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SpinSlots()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var result = await _minigameService.SpinSlotsAsync(userId);
            await _auditLogService.LogAsync(
                $"Minigame played. Game=slots; AwardedPoints={result.AwardedPoints}; WinningSpin={result.IsWinningSpin}; DailyPointsEarned={result.DailyPointsEarned}.",
                userId);

            return Json(new GameCompletionResultViewModel
            {
                GameKey = result.GameKey,
                AwardedPoints = result.AwardedPoints,
                CurrentPoints = result.CurrentPoints,
                Symbols = result.Symbols,
                IsWinningSpin = result.IsWinningSpin,
                ResultLabel = result.ResultLabel,
                DailyPointsEarned = result.DailyPointsEarned,
                DailyPointsLimit = result.DailyPointsLimit,
                HasReachedDailyLimit = result.HasReachedDailyLimit
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteGame([FromBody] CompleteGameRequestViewModel request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (request == null || !MinigameConstants.IsGenericCompletionGameKey(request.GameKey))
            {
                return BadRequest(new { message = "Invalid minigame key." });
            }

            var result = await _minigameService.CompleteGameAsync(userId, request.GameKey);
            await _auditLogService.LogAsync(
                $"Minigame played. Game={result.GameKey}; AwardedPoints={result.AwardedPoints}; DailyPointsEarned={result.DailyPointsEarned}.",
                userId);

            return Json(new GameCompletionResultViewModel
            {
                GameKey = result.GameKey,
                AwardedPoints = result.AwardedPoints,
                CurrentPoints = result.CurrentPoints,
                DailyPointsEarned = result.DailyPointsEarned,
                DailyPointsLimit = result.DailyPointsLimit,
                HasReachedDailyLimit = result.HasReachedDailyLimit
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTrivia([FromBody] SubmitTriviaRequestViewModel request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _minigameService.SubmitTriviaAnswerAsync(
                    userId,
                    new TriviaAnswerSubmission
                    {
                        QuestionId = request?.QuestionId ?? string.Empty,
                        SelectedOptionKey = request?.SelectedOptionKey ?? string.Empty
                    });

                await _auditLogService.LogAsync(
                    $"Minigame played. Game=trivia; AwardedPoints={result.AwardedPoints}; WasCorrect={result.WasCorrect}; RoundComplete={result.IsRoundComplete}; DailyPointsEarned={result.DailyPointsEarned}.",
                    userId);

                return Json(new GameCompletionResultViewModel
                {
                    GameKey = MinigameConstants.TriviaGameKey,
                    AwardedPoints = result.AwardedPoints,
                    CurrentPoints = result.CurrentPoints,
                    DailyPointsEarned = result.DailyPointsEarned,
                    DailyPointsLimit = result.DailyPointsLimit,
                    HasReachedDailyLimit = result.HasReachedDailyLimit,
                    WasCorrect = result.WasCorrect,
                    CorrectAnswers = result.CorrectAnswers,
                    CorrectAnswersToWin = result.CorrectAnswersToWin,
                    IsRoundComplete = result.IsRoundComplete,
                    NextQuestion = result.NextQuestion == null
                        ? null
                        : _minigameViewModelFactory.CreateTriviaQuestionViewModel(result.NextQuestion)
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
