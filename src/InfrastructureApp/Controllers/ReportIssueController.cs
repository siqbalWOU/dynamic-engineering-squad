/* The Report Issue Controller handles the Report Issue workflow: shows landing page, creates report, shows the created report*/

using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Services.ContentModeration;
using InfrastructureApp.Services.ImageHashing;

namespace InfrastructureApp.Controllers
{
    public class ReportIssueController : Controller
    {
        //dependency injection (business logic + identity for users)
        private readonly IReportIssueService _service;
        private readonly UserManager<Users> _userManager;
        private readonly IVoteService _voteService;
        private readonly IVerifyFixService _verifyFixService;
        private readonly IFlagService _flagService;

        public ReportIssueController(IReportIssueService service, UserManager<Users> userManager, IVoteService voteService, IVerifyFixService verifyFixService, IFlagService flagService)
        {
            _service = service;
            _userManager = userManager;
            _voteService = voteService;
            _verifyFixService = verifyFixService;
            _flagService = flagService;
        }

        //landing page
        [HttpGet]
        [Authorize]
        public IActionResult ReportIssue() => View();

        //shows the form to create a report +creates a fresh reportIssueViewModel and passes it into the view
        [HttpGet]
        [Authorize]
        public IActionResult Create(string? cameraId, string? imageUrl, decimal? lat, decimal? lng)
        {
            var report = new ReportIssue
            {
                CameraId = cameraId,
                CameraImageUrl = imageUrl,
                Latitude = lat,
                Longitude = lng
            };

            return View(report);
        }

        //runs when user submits the form
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(ReportIssue report)
        {
            // Check if the submitted form data failed validation
            // ModelState contains the results of all DataAnnotations validation
            // (Required, Range, StringLength, custom validation, etc.)
            if (!ModelState.IsValid)
            {
                //for testing validation errors
                // ModelState is a dictionary:
                // Key   = the name of the property (ex: "UserId", "Description")
                // Value = validation state + list of errors for that property
                foreach (var kvp in ModelState)
                {
                    // kvp.Key = the property name that failed validation
                    // Example: "UserId", "Latitude", "Photo"
                    var key = kvp.Key;

                    // kvp.Value.Errors = a list of validation errors for that property
                    // A property can have multiple errors (for example Required + Range)
                    var errors = kvp.Value.Errors;

                    foreach (var error in errors)
                    {
                        Console.WriteLine($"[ModelState] Key={key}, Error={error.ErrorMessage}");
                    }
                }


                // If validation failed, return the form view again
                // and pass the current model (report) back to the view.
                // This allows the user to see validation errors and fix them.
                return View(report);
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                userId = "user-guid-001";

            try
            {
                
                var (reportId, status) = await _service.CreateAsync(report, userId);

                TempData["Success"] = status == "Approved"
                    ? "XP gained! +10 points awarded."
                    : "Report submitted! It will appear on the map once moderation is complete.";

                TempData["SubmissionSuccess"] = true;   

                return RedirectToAction("Details", new { id = reportId });
            }
            catch (DuplicateImageException ex)
            {
                // Attach the message to the Photo field so it shows near the upload UI.
                ModelState.AddModelError(nameof(report.Photo), ex.Message);
                return View(report);
            }
            catch (ContentModerationRejectedException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(report);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(report);
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Something went wrong saving your report. Please try again.");
                return View(report);
            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkResolved(int id)
        {
            var found = await _service.UpdateStatusAsync(id, "Resolved");
            if (!found) return NotFound();

            TempData["Success"] = "Report marked as Resolved and added to the verify queue.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkVerifiedFixed(int id)
        {
            var found = await _service.UpdateStatusAsync(id, "Verified Fixed");
            if (!found) return NotFound();

            TempData["Success"] = "Report marked as Verified Fixed.";
            return RedirectToAction("Details", new { id });
        }

        //Shows the details page for a specific report id.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Load report through the service layer; return 404 if it doesn't exist.
            var report = await _service.GetByIdAsync(id);
            if (report == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var (voteCount, userHasVoted) = await _voteService.GetVoteStatusAsync(id, userId);
            ViewBag.VoteCount = voteCount;
            ViewBag.UserHasVoted = userHasVoted;

            var (verifyCount, userHasVerified) = await _verifyFixService.GetVerifyStatusAsync(id, userId);
            ViewBag.VerifyCount = verifyCount;
            ViewBag.UserHasVerified = userHasVerified;
            ViewBag.VerifyThreshold = VerifyFixService.VerificationThreshold;

            ViewBag.UserHasFlagged = userId != null && await _flagService.HasUserFlaggedAsync(id, userId);

            return View(report);
        }
    }
}
