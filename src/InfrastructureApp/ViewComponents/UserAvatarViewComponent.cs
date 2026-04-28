using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.ViewComponents
{
    public class UserAvatarViewComponent : ViewComponent
    {
        private readonly UserManager<Users> _userManager;

        public UserAvatarViewComponent(UserManager<Users> userManager)
        {
            _userManager = userManager;
        }

      public Task<IViewComponentResult> InvokeAsync()
        {
            return Task.FromResult<IViewComponentResult>(View("Default"));
        }
    }
}