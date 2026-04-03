using System.Security.Claims;
using InfrastructureApp.ViewModels.Account;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly SignInManager<Users> _signInManager;
        private readonly IUserService _userService;
        private readonly IAvatarService _avatarService;

        public AccountController(UserManager<Users> userManager,  SignInManager<Users> signInManager, IAvatarService avatarService, IUserService userService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _avatarService = avatarService;
            _userService = userService;
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
                return RedirectToAction("Index","Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
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

            var (success, error) = await _avatarService.SaveAvatarAsync(user, vm.SelectedAvatarKey);
            if (!success)
                return View(_avatarService.BuildChooseAvatarViewModel(user, vm.SelectedAvatarKey, error));

            await _signInManager.RefreshSignInAsync(user); 
            return RedirectToAction("Index", "Home");
        }
    }
}
