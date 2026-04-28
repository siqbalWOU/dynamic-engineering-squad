using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;

namespace InfrastructureApp_Tests.PointsShop
{
    [TestFixture]
    public class PointsShopControllerTests
    {
        [Test]
        public async Task Index_ReturnsViewResult_WithShopViewModel()
        {
            var serviceMock = new Mock<IPointsShopService>();
            serviceMock
                .Setup(s => s.GetShopAsync("user-1"))
                .ReturnsAsync(new PointsShopSnapshot
                {
                    CurrentPoints = 42,
                    Items = new List<PointsShopItemSummary>
                    {
                        new PointsShopItemSummary
                        {
                            Id = 1,
                            Name = "Golden Reporter Title",
                            Description = "desc",
                            CostPoints = 25,
                            IsSinglePurchase = true,
                            IsOwned = false,
                            CanPurchase = true
                        }
                    }
                });

            var controller = new PointsShopController(serviceMock.Object, CreateUserManager());
            controller.ControllerContext = BuildControllerContext("user-1");

            var result = await controller.Index();

            Assert.That(result, Is.TypeOf<ViewResult>());

            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.TypeOf<PointsShopIndexViewModel>());

            var model = (PointsShopIndexViewModel)viewResult.Model!;
            Assert.That(model.CurrentPoints, Is.EqualTo(42));
            Assert.That(model.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Purchase_WhenSuccessful_SetsSuccessMessage_AndRedirects()
        {
            var serviceMock = new Mock<IPointsShopService>();
            serviceMock
                .Setup(s => s.PurchaseAsync("user-1", 5))
                .ReturnsAsync(PointsShopPurchaseResult.Success("Purchased item.", 10, 5));

            var controller = new PointsShopController(serviceMock.Object, CreateUserManager())
            {
                ControllerContext = BuildControllerContext("user-1"),
                TempData = BuildTempData()
            };

            var result = await controller.Purchase(5);

            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            Assert.That(controller.TempData["Success"], Is.EqualTo("Purchased item."));
        }

        [Test]
        public async Task Purchase_WhenRejected_SetsErrorMessage_AndRedirects()
        {
            var serviceMock = new Mock<IPointsShopService>();
            serviceMock
                .Setup(s => s.PurchaseAsync("user-1", 9))
                .ReturnsAsync(PointsShopPurchaseResult.Failure("Not enough points.", 4, 9));

            var controller = new PointsShopController(serviceMock.Object, CreateUserManager())
            {
                ControllerContext = BuildControllerContext("user-1"),
                TempData = BuildTempData()
            };

            var result = await controller.Purchase(9);

            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            Assert.That(controller.TempData["Error"], Is.EqualTo("Not enough points."));
        }

        private static UserManager<Users> CreateUserManager()
        {
            var store = new Mock<IUserStore<Users>>();
            return new UserManager<Users>(
                store.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        private static ControllerContext BuildControllerContext(string userId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "shop-user")
            }, "TestAuth");

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        private static ITempDataDictionary BuildTempData()
        {
            return new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }
    }
}
