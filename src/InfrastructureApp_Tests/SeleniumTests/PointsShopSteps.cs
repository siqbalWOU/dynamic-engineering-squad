using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace InfrastructureApp_Tests.SeleniumTests
{
    [Binding]
    public class PointsShopSteps : SeleniumTestBase
    {
        [Given(@"a points shop user ""(.*)"" with password ""(.*)"" and current balance (.*) exists")]
        public async Task GivenAPointsShopUserWithPasswordAndCurrentBalanceExists(string username, string password, int currentBalance)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existingUser = await userManager.FindByNameAsync(username);
            if (existingUser != null)
            {
                var existingPoints = await db.UserPoints.FirstOrDefaultAsync(points => points.UserId == existingUser.Id);
                if (existingPoints != null)
                {
                    db.UserPoints.Remove(existingPoints);
                }

                var existingPurchases = await db.UserShopItemPurchases
                    .Where(purchase => purchase.UserId == existingUser.Id)
                    .ToListAsync();

                if (existingPurchases.Count > 0)
                {
                    db.UserShopItemPurchases.RemoveRange(existingPurchases);
                }

                await db.SaveChangesAsync();
                await userManager.DeleteAsync(existingUser);
            }

            var user = new Users
            {
                UserName = username,
                Email = $"{username}@example.com",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create test user '{username}': {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
            }

            db.UserPoints.Add(new UserPoints
            {
                UserId = user.Id,
                CurrentPoints = currentBalance,
                LifetimePoints = currentBalance,
                LastUpdated = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            await EnsureCatalogSeededAsync(user.Id);
        }

        [Given(@"the user ""(.*)"" already owns the points shop item ""(.*)""")]
        public async Task GivenTheUserAlreadyOwnsThePointsShopItem(string username, string itemName)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await userManager.FindByNameAsync(username)
                ?? throw new InvalidOperationException($"User '{username}' was not found.");

            await EnsureCatalogSeededAsync(user.Id);

            var item = await db.ShopItems.SingleAsync(shopItem => shopItem.Name == itemName);
            var existingPurchase = await db.UserShopItemPurchases
                .FirstOrDefaultAsync(purchase => purchase.UserId == user.Id && purchase.ShopItemId == item.Id);

            if (existingPurchase == null)
            {
                db.UserShopItemPurchases.Add(new UserShopItemPurchase
                {
                    UserId = user.Id,
                    ShopItemId = item.Id,
                    CostPoints = item.CostPoints,
                    PurchasedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }
        }

        [When(@"I sign in to the points shop as ""(.*)"" with password ""(.*)""")]
        public void WhenISignInToThePointsShopAsWithPassword(string username, string password)
        {
            Login(username, password);
        }

        [When(@"I open the Points Shop page")]
        public void WhenIOpenThePointsShopPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/PointsShop");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => driver.FindElement(By.CssSelector("[data-testid='points-shop-page']")));
        }

        [When(@"I purchase the points shop item ""(.*)""")]
        public void WhenIPurchaseThePointsShopItem(string itemName)
        {
            var card = FindShopItemCard(itemName);
            var button = card.FindElement(By.CssSelector("[data-testid='points-shop-purchase-button']"));
            ScrollAndClick(button);
        }

        [When(@"I attempt to purchase the points shop item ""(.*)""")]
        public void WhenIAttemptToPurchaseThePointsShopItem(string itemName)
        {
            var card = FindShopItemCard(itemName);
            var form = card.FindElement(By.CssSelector("[data-testid='points-shop-purchase-form']"));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].submit();", form);
        }

        [Then(@"the Points Shop page should load successfully")]
        public void ThenThePointsShopPageShouldLoadSuccessfully()
        {
            Assert.That(Driver.Url, Does.Contain("/PointsShop"));
            Assert.That(Driver.FindElement(By.TagName("h1")).Text, Does.Contain("Points Shop"));
        }

        [Then(@"the points shop should show a current balance of (.*)")]
        public void ThenThePointsShopShouldShowACurrentBalanceOf(int expectedBalance)
        {
            var balance = Driver.FindElement(By.CssSelector("[data-testid='points-shop-current-balance']")).Text;
            Assert.That(balance, Is.EqualTo(expectedBalance.ToString()));
        }

        [Then(@"the points shop should show the item ""(.*)""")]
        public void ThenThePointsShopShouldShowTheItem(string itemName)
        {
            var card = FindShopItemCard(itemName);
            var name = card.FindElement(By.CssSelector("[data-testid='points-shop-item-name']")).Text;
            Assert.That(name, Is.EqualTo(itemName));
        }

        [Then(@"the points shop item ""(.*)"" should show description ""(.*)""")]
        public void ThenThePointsShopItemShouldShowDescription(string itemName, string expectedDescription)
        {
            var card = FindShopItemCard(itemName);
            var description = card.FindElement(By.CssSelector("[data-testid='points-shop-item-description']")).Text;
            Assert.That(description, Is.EqualTo(expectedDescription));
        }

        [Then(@"the points shop item ""(.*)"" should show cost (.*)")]
        public void ThenThePointsShopItemShouldShowCost(string itemName, int expectedCost)
        {
            var card = FindShopItemCard(itemName);
            var cost = card.FindElement(By.CssSelector("[data-testid='points-shop-item-cost']")).Text;
            Assert.That(cost, Is.EqualTo($"{expectedCost} pts"));
        }

        [Then(@"the points shop should show a success message containing ""(.*)""")]
        public void ThenThePointsShopShouldShowASuccessMessageContaining(string expectedMessage)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var message = wait.Until(driver => driver.FindElement(By.CssSelector("[data-testid='points-shop-success']")));
            Assert.That(message.Text, Does.Contain(expectedMessage));
        }

        [Then(@"the points shop should show an error message containing ""(.*)""")]
        public void ThenThePointsShopShouldShowAnErrorMessageContaining(string expectedMessage)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var message = wait.Until(driver => driver.FindElement(By.CssSelector("[data-testid='points-shop-error']")));
            Assert.That(message.Text, Does.Contain(expectedMessage));
        }

        [Then(@"the user ""(.*)"" should own the points shop item ""(.*)""")]
        public async Task ThenTheUserShouldOwnThePointsShopItem(string username, string itemName)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await userManager.FindByNameAsync(username)
                ?? throw new InvalidOperationException($"User '{username}' was not found.");
            var item = await db.ShopItems.SingleAsync(shopItem => shopItem.Name == itemName);

            var ownsItem = await db.UserShopItemPurchases
                .AnyAsync(purchase => purchase.UserId == user.Id && purchase.ShopItemId == item.Id);

            Assert.That(ownsItem, Is.True);
        }

        [Then(@"the user ""(.*)"" should not own the points shop item ""(.*)""")]
        public async Task ThenTheUserShouldNotOwnThePointsShopItem(string username, string itemName)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await userManager.FindByNameAsync(username)
                ?? throw new InvalidOperationException($"User '{username}' was not found.");
            var item = await db.ShopItems.SingleAsync(shopItem => shopItem.Name == itemName);

            var ownsItem = await db.UserShopItemPurchases
                .AnyAsync(purchase => purchase.UserId == user.Id && purchase.ShopItemId == item.Id);

            Assert.That(ownsItem, Is.False);
        }

        [Then(@"the user ""(.*)"" should have exactly (.*) purchase record for the points shop item ""(.*)""")]
        public async Task ThenTheUserShouldHaveExactlyPurchaseRecordForThePointsShopItem(string username, int expectedCount, string itemName)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await userManager.FindByNameAsync(username)
                ?? throw new InvalidOperationException($"User '{username}' was not found.");
            var item = await db.ShopItems.SingleAsync(shopItem => shopItem.Name == itemName);

            var purchaseCount = await db.UserShopItemPurchases
                .CountAsync(purchase => purchase.UserId == user.Id && purchase.ShopItemId == item.Id);

            Assert.That(purchaseCount, Is.EqualTo(expectedCount));
        }

        [Then(@"the points shop item ""(.*)"" should be unavailable for purchase")]
        public void ThenThePointsShopItemShouldBeUnavailableForPurchase(string itemName)
        {
            var card = FindShopItemCard(itemName);
            var button = card.FindElement(By.CssSelector("[data-testid='points-shop-purchase-button']"));
            var status = card.FindElement(By.CssSelector("[data-testid='points-shop-item-status']")).Text;

            Assert.That(button.Enabled, Is.False);
            Assert.That(status, Does.Contain("Not enough points"));
        }

        [Then(@"the points shop item ""(.*)"" should be marked as owned")]
        public void ThenThePointsShopItemShouldBeMarkedAsOwned(string itemName)
        {
            var card = FindShopItemCard(itemName);
            var button = card.FindElement(By.CssSelector("[data-testid='points-shop-purchase-button']"));
            var status = card.FindElement(By.CssSelector("[data-testid='points-shop-item-status']")).Text;

            Assert.That(button.Enabled, Is.False);
            Assert.That(status, Does.Contain("Owned"));
        }

        private static async Task EnsureCatalogSeededAsync(string userId)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var pointsShopService = scope.ServiceProvider.GetRequiredService<IPointsShopService>();
            await pointsShopService.GetShopAsync(userId);
        }

        private IWebElement FindShopItemCard(string itemName)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            return wait.Until(driver =>
            {
                var cards = driver.FindElements(By.CssSelector("[data-testid='points-shop-item']"));
                return cards.FirstOrDefault(card =>
                    string.Equals(card.GetAttribute("data-shop-item-name"), itemName, StringComparison.Ordinal));
            }) ?? throw new NoSuchElementException($"Points shop item '{itemName}' was not found.");
        }
    }
}
