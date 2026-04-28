using System.Security.Claims;
using InfrastructureApp.ViewModels.Account;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace InfrastructureApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly SignInManager<Users> _signInManager;
        private readonly IUserService _userService;
        private readonly IAvatarService _avatarService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<Users> userManager, SignInManager<Users> signInManager, IAvatarService avatarService, IUserService userService, IEmailService emailService, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _avatarService = avatarService;
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await _signInManager
                .PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            if (result.IsNotAllowed)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "You must confirm your email before logging in.");
                    ViewBag.ShowResendButton = true;
                    ViewBag.UserId = user.Id;
                    return View(model);
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new Users()
            {
                UserName = model.Username,
                Email = model.Email,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action("ConfirmEmail", "Account", 
                    new { userId = user.Id, token = token }, protocol: HttpContext.Request.Scheme);

                await _emailService.SendEmailAsync(model.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{callbackUrl}'>clicking here</a>.");

                return RedirectToAction("RegisterConfirmation", new { email = model.Email });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterConfirmation(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return View("ErrorConfirmingEmail");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action("ConfirmEmail", "Account", 
                new { userId = user.Id, token = token }, protocol: HttpContext.Request.Scheme);

            await _emailService.SendEmailAsync(user.Email!, "Confirm your email",
                $"Please confirm your account by <a href='{callbackUrl}'>clicking here</a>.");

            ViewBag.Message = "Verification email sent. Please check your inbox.";
            return View("RegisterConfirmation", new { email = user.Email });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin(int page = 1)
        {
            var pageSize = 10;
            var model = await _userService.GetUsersWithRolesAsync(page, pageSize);
            return View(model);
        }
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditRoles(string userId)
        {
            var model = await _userService.GetManageRolesViewModelAsync(userId);
            return model == null ? NotFound() : View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditRoles(ManageUserRolesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            string currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _userService.UpdateUserRolesAsync(model, currentAdminId);
        
            if (result.Succeeded) return RedirectToAction("Admin");

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                TempData["ForgotPasswordMessage"] = "If your email is in our system, you will receive a reset link shortly.";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);
            
            await _emailService.SendEmailAsync(model.Email, "Reset Password",
                $"Please reset your password by <a href='{callbackUrl}'>clicking here</a>.");

            TempData["ForgotPasswordMessage"] = "If your email is in our system, you will receive a reset link shortly.";
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (token == null || email == null)
            {
                return BadRequest("A token and email must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest("Invalid or expired token.");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["ResetPasswordMessage"] = "Your password has been reset successfully.";
                return View();
            }

            if (result.Errors.Any(e => e.Code == "InvalidToken"))
            {
                return BadRequest("Invalid or expired token.");
            }

            foreach (var errorItem in result.Errors)
            {
                ModelState.AddModelError(string.Empty, errorItem.Description);
            }
            TempData["ResetPasswordErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));

            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult DeleteAccount()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _userService.DeleteAccountAsync(currentUserId, model.CurrentPassword);

            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            string currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _userService.DeleteUserAsync(userId, currentAdminId);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
                return RedirectToAction("Admin");
            }

            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction("Admin");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChooseAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var vm = _avatarService.BuildChooseAvatarViewModel(user);
            return View(vm);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChooseAvatar(ChooseAvatarViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            (bool Success, string? ErrorMessage) result;

            if (vm.UseUploadedImage)
            {
                result = await _avatarService.SaveUploadedAvatarAsync(user, vm.UploadedImage);
            }
            else
            {
                result = await _avatarService.SaveAvatarAsync(user, vm.SelectedAvatarKey);
            }

            if (!result.Success)
            {
                return View(_avatarService.BuildChooseAvatarViewModel(user, vm.SelectedAvatarKey, result.ErrorMessage));
            }

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction("Index", "Home");
        }
    }
}
