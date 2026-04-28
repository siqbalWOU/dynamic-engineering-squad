using System;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Dashboard
{
    [TestFixture]
    public class DashboardRepositoryEfTests
    {
        private ApplicationDbContext _db = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("DashboardRepositoryTest_" + Guid.NewGuid())
                .Options;

            _db = new ApplicationDbContext(options);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task GetPublicProfileAsync_WhenDashboardBackgroundPurchased_ReturnsBackgroundUrl()
        {
            var selectedBackground = PointsShopCatalog.GetDashboardBackgroundByKey("city-grid")!;
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "alice",
                Email = "alice@test.com",
                EmailConfirmed = true,
                SelectedDashboardBackgroundKey = selectedBackground.Key
            };
            _db.Users.Add(user);

            var backgroundItem = new ShopItem
            {
                Name = selectedBackground.Name,
                Description = "Background unlock",
                CostPoints = 10,
                IsSinglePurchase = true,
                IsActive = true
            };
            _db.ShopItems.Add(backgroundItem);
            await _db.SaveChangesAsync();

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = user.Id,
                ShopItemId = backgroundItem.Id,
                CostPoints = backgroundItem.CostPoints,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var result = await repo.GetPublicProfileAsync("alice");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.PersonalInfoBackgroundUrl, Is.EqualTo(selectedBackground.ImageUrl));
            Assert.That(result.HasPersonalInfoBackground, Is.True);
        }

        [Test]
        public async Task GetPublicProfileAsync_WhenDashboardBackgroundNotPurchased_ReturnsNoBackgroundUrl()
        {
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "bob",
                Email = "bob@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var result = await repo.GetPublicProfileAsync("bob");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.PersonalInfoBackgroundUrl, Is.Null);
            Assert.That(result.HasPersonalInfoBackground, Is.False);
        }

        [Test]
        public async Task GetPublicProfileAsync_WhenDashboardBorderPurchased_ReturnsBorderCssClass()
        {
            var selectedBorder = PointsShopCatalog.GetDashboardBorderByKey("gold-ring")!;
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "erin",
                Email = "erin@test.com",
                EmailConfirmed = true,
                SelectedDashboardBorderKey = selectedBorder.Key
            };
            _db.Users.Add(user);

            var borderItem = new ShopItem
            {
                Name = selectedBorder.Name,
                Description = selectedBorder.Description,
                CostPoints = 10,
                IsSinglePurchase = true,
                IsActive = true
            };
            _db.ShopItems.Add(borderItem);
            await _db.SaveChangesAsync();

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = user.Id,
                ShopItemId = borderItem.Id,
                CostPoints = 10,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var result = await repo.GetPublicProfileAsync("erin");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.SelectedDashboardBorderKey, Is.EqualTo(selectedBorder.Key));
            Assert.That(result.PersonalInfoBorderCssClass, Is.EqualTo(selectedBorder.CssClass));
        }

        [Test]
        public async Task UpdateSelectedDashboardBackgroundAsync_WhenUnlocked_UpdatesUserPreference()
        {
            var background = PointsShopCatalog.GetDashboardBackgroundByKey("blueprint")!;
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "charlie",
                Email = "charlie@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);

            var item = new ShopItem
            {
                Name = background.Name,
                Description = background.Description,
                CostPoints = 10,
                IsSinglePurchase = true,
                IsActive = true
            };
            _db.ShopItems.Add(item);
            await _db.SaveChangesAsync();

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = user.Id,
                ShopItemId = item.Id,
                CostPoints = 10,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var updated = await repo.UpdateSelectedDashboardBackgroundAsync(user.Id, background.Key);

            Assert.That(updated, Is.True);
            Assert.That(user.SelectedDashboardBackgroundKey, Is.EqualTo(background.Key));
        }

        [Test]
        public async Task UpdateSelectedDashboardBackgroundAsync_WhenLocked_ReturnsFalse()
        {
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "dana",
                Email = "dana@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var updated = await repo.UpdateSelectedDashboardBackgroundAsync(user.Id, "signal-glow");

            Assert.That(updated, Is.False);
            Assert.That(user.SelectedDashboardBackgroundKey, Is.Null);
        }

        [Test]
        public async Task UpdateSelectedActivitySummaryBackgroundAsync_WhenUnlocked_UpdatesUserPreference()
        {
            var background = PointsShopCatalog.GetActivitySummaryBackgroundByKey("signal-grid")!;
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "helen",
                Email = "helen@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);

            var item = new ShopItem
            {
                Name = background.Name,
                Description = background.Description,
                CostPoints = 10,
                IsSinglePurchase = true,
                IsActive = true
            };
            _db.ShopItems.Add(item);
            await _db.SaveChangesAsync();

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = user.Id,
                ShopItemId = item.Id,
                CostPoints = 10,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var updated = await repo.UpdateSelectedActivitySummaryBackgroundAsync(user.Id, background.Key);

            Assert.That(updated, Is.True);
            Assert.That(user.SelectedActivitySummaryBackgroundKey, Is.EqualTo(background.Key));
        }

        [Test]
        public async Task GetDashboardSummaryAsync_WhenActivitySummaryBackgroundPurchased_ReturnsBackgroundUrl()
        {
            var background = PointsShopCatalog.GetActivitySummaryBackgroundByKey("amber-wave")!;
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "ivy",
                Email = "ivy@test.com",
                EmailConfirmed = true,
                SelectedActivitySummaryBackgroundKey = background.Key
            };
            _db.Users.Add(user);

            var item = new ShopItem
            {
                Name = background.Name,
                Description = background.Description,
                CostPoints = 10,
                IsSinglePurchase = true,
                IsActive = true
            };
            _db.ShopItems.Add(item);
            await _db.SaveChangesAsync();

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = user.Id,
                ShopItemId = item.Id,
                CostPoints = 10,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id)
                }, "TestAuth"));
            httpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);

            var userManager = CreateUserManager(user);
            Mock.Get(userManager)
                .Setup(m => m.GetUserAsync(httpContext.User))
                .ReturnsAsync(user);

            var repo = new DashboardRepositoryEf(_db, userManager, httpContextAccessor.Object);

            var result = await repo.GetDashboardSummaryAsync();

            Assert.That(result.ActivitySummaryBackgroundUrl, Is.EqualTo(background.ImageUrl));
            Assert.That(result.SelectedActivitySummaryBackgroundKey, Is.EqualTo(background.Key));
        }

        [Test]
        public async Task UpdateSelectedActivitySummaryBorderAsync_WhenUnlocked_UpdatesUserPreference()
        {
            var border = PointsShopCatalog.GetActivitySummaryBorderByKey("signal-ring")!;
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "jane",
                Email = "jane@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);

            var item = new ShopItem
            {
                Name = border.Name,
                Description = border.Description,
                CostPoints = 10,
                IsSinglePurchase = true,
                IsActive = true
            };
            _db.ShopItems.Add(item);
            await _db.SaveChangesAsync();

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = user.Id,
                ShopItemId = item.Id,
                CostPoints = 10,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var updated = await repo.UpdateSelectedActivitySummaryBorderAsync(user.Id, border.Key);

            Assert.That(updated, Is.True);
            Assert.That(user.SelectedActivitySummaryBorderKey, Is.EqualTo(border.Key));
        }

        [Test]
        public async Task GetDashboardSummaryAsync_WhenActivitySummaryBorderPurchased_ReturnsBorderCssClass()
        {
            var border = PointsShopCatalog.GetActivitySummaryBorderByKey("amber-rail")!;
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "kate",
                Email = "kate@test.com",
                EmailConfirmed = true,
                SelectedActivitySummaryBorderKey = border.Key
            };
            _db.Users.Add(user);

            var item = new ShopItem
            {
                Name = border.Name,
                Description = border.Description,
                CostPoints = 10,
                IsSinglePurchase = true,
                IsActive = true
            };
            _db.ShopItems.Add(item);
            await _db.SaveChangesAsync();

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = user.Id,
                ShopItemId = item.Id,
                CostPoints = 10,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id)
                }, "TestAuth"));
            httpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);

            var userManager = CreateUserManager(user);
            Mock.Get(userManager)
                .Setup(m => m.GetUserAsync(httpContext.User))
                .ReturnsAsync(user);

            var repo = new DashboardRepositoryEf(_db, userManager, httpContextAccessor.Object);

            var result = await repo.GetDashboardSummaryAsync();

            Assert.That(result.ActivitySummaryBorderCssClass, Is.EqualTo(border.CssClass));
            Assert.That(result.SelectedActivitySummaryBorderKey, Is.EqualTo(border.Key));
        }

        [Test]
        public async Task UpdateSelectedDashboardBorderAsync_WhenUnlocked_UpdatesUserPreference()
        {
            var border = PointsShopCatalog.GetDashboardBorderByKey("steel-frame")!;
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "frank",
                Email = "frank@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);

            var item = new ShopItem
            {
                Name = border.Name,
                Description = border.Description,
                CostPoints = 10,
                IsSinglePurchase = true,
                IsActive = true
            };
            _db.ShopItems.Add(item);
            await _db.SaveChangesAsync();

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = user.Id,
                ShopItemId = item.Id,
                CostPoints = 10,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var updated = await repo.UpdateSelectedDashboardBorderAsync(user.Id, border.Key);

            Assert.That(updated, Is.True);
            Assert.That(user.SelectedDashboardBorderKey, Is.EqualTo(border.Key));
        }

        [Test]
        public async Task UpdateSelectedDashboardBorderAsync_WhenLocked_ReturnsFalse()
        {
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "gina",
                Email = "gina@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var updated = await repo.UpdateSelectedDashboardBorderAsync(user.Id, "ember-outline");

            Assert.That(updated, Is.False);
            Assert.That(user.SelectedDashboardBorderKey, Is.Null);
        }

        private static UserManager<Users> CreateUserManager(Users user)
        {
            var store = new Mock<IUserStore<Users>>();
            var userManager = new Mock<UserManager<Users>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManager.Setup(m => m.FindByNameAsync(user.UserName!))
                .ReturnsAsync(user);
            userManager.Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);
            userManager.Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            return userManager.Object;
        }
    }
}
