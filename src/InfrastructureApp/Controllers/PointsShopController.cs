using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    [Authorize]
    public class PointsShopController : Controller
    {
        private readonly IPointsShopService _pointsShopService;
        private readonly UserManager<Users> _userManager;

        public PointsShopController(IPointsShopService pointsShopService, UserManager<Users> userManager)
        {
            _pointsShopService = pointsShopService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var snapshot = await _pointsShopService.GetShopAsync(userId);
            return View(BuildViewModel(snapshot));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var result = await _pointsShopService.PurchaseAsync(userId, id);

            if (result.Succeeded)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private static PointsShopIndexViewModel BuildViewModel(PointsShopSnapshot snapshot)
        {
            return new PointsShopIndexViewModel
            {
                CurrentPoints = snapshot.CurrentPoints,
                Items = snapshot.Items
                    .Select(item => new PointsShopItemViewModel
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Description = item.Description,
                        CostPoints = item.CostPoints,
                        IsSinglePurchase = item.IsSinglePurchase,
                        IsOwned = item.IsOwned,
                        CanPurchase = item.CanPurchase,
                        CategoryLabel = item.CategoryLabel,
                        PreviewImageUrl = item.PreviewImageUrl,
                        PreviewCssClass = item.PreviewCssClass
                    })
                    .ToList()
                    .AsReadOnly()
            };
        }
    }
}
