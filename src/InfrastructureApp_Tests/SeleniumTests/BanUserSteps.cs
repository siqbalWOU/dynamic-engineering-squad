using InfrastructureApp.Models;
using InfrastructureApp.Data;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using NUnit.Framework;
using OpenQA.Selenium.Support.UI;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class BanUserSteps : SeleniumTestBase
    {
        private readonly ScenarioContext _scenarioContext;

        public BanUserSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [Given(@"""(.*)"" is banned for ""(.*)""")]
        public async Task GivenIsBannedFor(string username, string reason)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            
            var user = await userManager.FindByNameAsync(username);
            Assert.That(user, Is.Not.Null, $"User {username} not found in DB.");

            user.IsBanned = true;
            user.BanReason = reason;
            // Update security stamp to invalidate sessions
            await userManager.UpdateSecurityStampAsync(user);
            var result = await userManager.UpdateAsync(user);
            Assert.That(result.Succeeded, Is.True, $"Failed to ban user {username} in DB.");
        }

        [Then(@"I should see a ""Ban"" button for user ""(.*)""")]
        public void ThenIShouldSeeABanButtonForUser(string username)
        {
            EnsureUserIsVisibleOnAdminPage(username);
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var banButton = wait.Until(d => d.FindElement(By.XPath($"//button[@data-username='{username}' and contains(normalize-space(), 'Ban')]")));
            Assert.That(banButton.Displayed, Is.True, $"Ban button for user {username} is not displayed.");
        }

        [When(@"I click ""Ban"" for user ""(.*)""")]
        public void WhenIClickBanForUser(string username)
        {
            EnsureUserIsVisibleOnAdminPage(username);
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var banButton = wait.Until(d => d.FindElement(By.XPath($"//button[@data-username='{username}' and contains(normalize-space(), 'Ban')]")));
            ScrollAndClick(banButton);
        }

        [Then(@"I should see a ban confirmation modal for ""(.*)""")]
        public void ThenIShouldSeeABanConfirmationModalFor(string username)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var modal = wait.Until(d => {
                var m = d.FindElement(By.Id("banModal"));
                return m.Displayed ? m : null;
            });
            Assert.That(modal.Text, Does.Contain($"Ban {username}"));
        }

        [When(@"I enter ""(.*)"" as the ban reason")]
        public void WhenIEnterAsTheBanReason(string reason)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var reasonInput = wait.Until(d => d.FindElement(By.Id("BanReason")));
            reasonInput.Clear();
            reasonInput.SendKeys(reason);
        }

        [When(@"I confirm the ban")]
        public void WhenIConfirmTheBan()
        {
            var confirmButton = Driver.FindElement(By.Id("confirmBanButton"));
            confirmButton.Click();
            
            // Wait for modal to close or page to navigate away
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            wait.Until(d => {
                try {
                    var m = d.FindElement(By.Id("banModal"));
                    return !m.Displayed;
                } catch (NoSuchElementException) {
                    return true;
                } catch (StaleElementReferenceException) {
                    return true;
                }
            });
        }

        [Then(@"I should see ""Unban"" instead of ""Ban"" for user ""(.*)""")]
        public void ThenIShouldSeeUnbanInsteadOfBanForUser(string username)
        {
            EnsureUserIsVisibleOnAdminPage(username);
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            
            var unbanButton = wait.Until(d => d.FindElement(By.XPath($"//button[@data-username='{username}' and contains(normalize-space(), 'Unban')]")));
            Assert.That(unbanButton.Displayed, Is.True, $"Unban button for user {username} not found.");
            
            var banButtons = Driver.FindElements(By.XPath($"//button[@data-username='{username}' and normalize-space()='Ban']"));
            Assert.That(banButtons.Count, Is.EqualTo(0), $"Ban button for user {username} should not be present.");
        }

        [When(@"""(.*)"" bans ""(.*)"" for ""(.*)""")]
        public async Task WhenBansFor(string adminUsername, string targetUsername, string reason)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            
            var targetUser = await userManager.FindByNameAsync(targetUsername);
            var adminUser = await userManager.FindByNameAsync(adminUsername);

            Assert.That(targetUser, Is.Not.Null);
            Assert.That(adminUser, Is.Not.Null);

            await userService.BanUserAsync(targetUser.Id, adminUser.Id, reason);
        }

        [When(@"I click ""Unban"" for user ""(.*)""")]
        public void WhenIClickUnbanForUser(string username)
        {
            EnsureUserIsVisibleOnAdminPage(username);
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var unbanButton = wait.Until(d => d.FindElement(By.XPath($"//button[@data-username='{username}' and contains(normalize-space(), 'Unban')]")));
            ScrollAndClick(unbanButton);
        }

        [Then("I should see an error message {string}")]
        [Scope(Feature = "Ban User")]
        public void ThenIShouldSeeAnErrorMessage(string expectedMessage)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
            try
            {
                wait.Until(d => {
                    try {
                        return d.FindElement(By.TagName("body")).Text.Contains(expectedMessage);
                    } catch (StaleElementReferenceException) {
                        return false;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Error message '{expectedMessage}' not found. Current URL: {Driver.Url}");
                Console.WriteLine("Page Source:");
                Console.WriteLine(Driver.PageSource);
                throw;
            }
            var bodyText = Driver.FindElement(By.TagName("body")).Text;
            Assert.That(bodyText, Does.Contain(expectedMessage));
        }

        [Then(@"a moderation action should be logged for ""(.*)"" ""(.*)""")]
        [Scope(Feature = "Ban User")]
        public void ThenAModerationActionShouldBeLoggedFor(string action, string username)
        {
             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
             wait.Until(d => {
                 using var scope = ServerHost!.Services.CreateScope();
                 var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                 return db.ModerationActionLogs.Any(l => l.Action == action && l.TargetContentSnapshot.Contains(username));
             });
        }

        private void EnsureUserIsVisibleOnAdminPage(string username)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
            
            // Wait for any element that indicates the page has loaded (either table or access denied)
            wait.Until(d => d.FindElements(By.TagName("table")).Count > 0 || d.PageSource.Contains("Access Denied"));

            if (Driver.PageSource.Contains("Access Denied"))
            {
                Assert.Fail("Access Denied to Admin page.");
            }

            int maxPages = 20; 
            int currentPage = 1;

            while (currentPage <= maxPages)
            {
                var userRows = Driver.FindElements(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]"));
                if (userRows.Count > 0) return;

                var nextButtons = Driver.FindElements(By.XPath("//li[contains(@class, 'page-item') and not(contains(@class, 'disabled'))]/a[contains(normalize-space(), 'Next')]"));
                if (nextButtons.Count == 0)
                {
                    break;
                }
                
                var oldTable = Driver.FindElement(By.TagName("table"));
                ScrollAndClick(nextButtons[0]);
                
                // Wait for the table to be replaced or content to change
                wait.Until(d => {
                    try {
                        var newTable = d.FindElement(By.TagName("table"));
                        return !newTable.Equals(oldTable);
                    } catch (StaleElementReferenceException) {
                        return true;
                    } catch {
                        return false;
                    }
                });
                
                currentPage++;
            }
        }
    }
}
