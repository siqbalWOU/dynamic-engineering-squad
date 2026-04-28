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

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class RemoveUserSteps : SeleniumTestBase
    {
        private readonly ScenarioContext _scenarioContext;

        public RemoveUserSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [Given(@"the user ""(.*)"" has the ""(.*)"" role")]
        public async Task GivenTheUserHasTheRole(string username, string roleName)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var user = await userManager.FindByNameAsync(username);
            Assert.That(user, Is.Not.Null, $"User {username} not found");

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                await userManager.AddToRoleAsync(user, roleName);
            }
        }

        [When(@"I log in with username ""(.*)"" and password ""(.*)"" for RemoveUser")]
        public void WhenILogInWithUsernameAndPasswordForRemoveUser(string username, string password)
        {
            Login(username, password);
        }

        [When(@"I navigate to the Admin page")]
        public void WhenINavigateToTheAdminPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Admin");
        }

        [Then(@"I should see a ""Remove"" button for user ""(.*)""")]
        public void ThenIShouldSeeARemoveButtonForUser(string username)
        {
            EnsureUserIsVisibleOnAdminPage(username);
            var removeButtons = Driver.FindElements(By.XPath($"//tr[td[contains(text(), '{username}')]]//button[contains(text(), 'Remove')] | //tr[td[contains(text(), '{username}')]]//a[contains(text(), 'Remove')]"));
            if (removeButtons.Count == 0)
            {
                Console.WriteLine($"Remove button for {username} not found. Current URL: {Driver.Url}");
                Console.WriteLine("Page Source:");
                Console.WriteLine(Driver.PageSource);
            }
            Assert.That(removeButtons.Count, Is.GreaterThan(0), $"Remove button for user {username} not found.");
        }

        private void EnsureUserIsVisibleOnAdminPage(string username)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            while (true)
            {
                var userRows = Driver.FindElements(By.XPath($"//tr[td[contains(text(), '{username}')]]"));
                if (userRows.Count > 0) return;

                var nextButtons = Driver.FindElements(By.XPath("//li[contains(@class, 'page-item') and not(contains(@class, 'disabled'))]/a[contains(text(), 'Next')]"));
                if (nextButtons.Count == 0)
                {
                    break;
                }
                ScrollAndClick(nextButtons[0]);
                Thread.Sleep(500); // Wait for page load
            }
        }

        [Then(@"I should see an error message ""(.*)"" or be redirected")]
        public void ThenIShouldSeeAnErrorMessageOrBeRedirected(string expectedMessage)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            try
            {
                wait.Until(d => d.Url.Contains("/Account/Login") || d.FindElement(By.TagName("body")).Text.Contains(expectedMessage));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Error message or redirection not found. Current URL: {Driver.Url}");
                Console.WriteLine("Page Source:");
                Console.WriteLine(Driver.PageSource);
                Assert.Fail($"Neither redirected to login nor saw error message '{expectedMessage}'. Current URL: {Driver.Url}");
            }
        }

        [When(@"I click ""Remove"" for user ""(.*)""")]
        public void WhenIClickRemoveForUser(string username)
        {
            try
            {
                EnsureUserIsVisibleOnAdminPage(username);
                var removeButton = Driver.FindElement(By.XPath($"//tr[td[contains(text(), '{username}')]]//*[contains(text(), 'Remove')]"));
                removeButton.Click();
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine($"Clicking 'Remove' for {username} failed. Current URL: {Driver.Url}");
                Console.WriteLine("Page Source:");
                Console.WriteLine(Driver.PageSource);
                throw;
            }
        }

        [Then(@"I should see a confirmation modal for ""(.*)""")]
        public void ThenIShouldSeeAConfirmationModalFor(string username)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            try
            {
                var modal = wait.Until(d => {
                    var m = d.FindElement(By.ClassName("modal"));
                    return m.Displayed ? m : null;
                });
                Assert.That(modal, Is.Not.Null);
                Assert.That(modal.Text, Does.Contain($"Are you sure you want to delete {username}"));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Confirmation modal for {username} not found or not visible. Current URL: {Driver.Url}");
                Console.WriteLine("Page Source:");
                Console.WriteLine(Driver.PageSource);
                throw;
            }
        }

        [When(@"I confirm the deletion")]
        public void WhenIConfirmTheDeletion()
        {
            var confirmButton = Driver.FindElement(By.Id("confirmDeleteButton"));
            confirmButton.Click();
        }

        [Then(@"I should not see ""(.*)"" in the user list")]
        public void ThenIShouldNotSeeInTheUserList(string username)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));

            try
            {
                // wait.Until returns true when it succeeds. Capture it directly.
                bool messageFound = wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains(username));
                Assert.That(messageFound, Is.True, $"Error message '{username}' was not found.");
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"User {username} still visible. Current URL: {Driver.Url}");
                Console.WriteLine("Page Source:");
                Console.WriteLine(Driver.PageSource);
            }
            var userRow = Driver.FindElements(By.XPath($"//tr[td[contains(text(), '{username}')]]"));
            Assert.That(userRow.Count, Is.EqualTo(0), $"User {username} still found in the list.");
        }

        [Then(@"I should not see a ""Remove"" button for user ""(.*)""")]
        public void ThenIShouldNotSeeARemoveButtonForUser(string username)
        {
            var removeButtons = Driver.FindElements(By.XPath($"//tr[td[contains(text(), '{username}')]]//*[contains(text(), 'Remove')]"));
            Assert.That(removeButtons.Count, Is.EqualTo(0), $"Remove button for user {username} should not be present.");
        }

        [StepDefinition(@"I am authenticated")]
        public void IAmAuthenticated()
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            try
            {
                wait.Until(d => d.FindElements(By.XPath("//a[contains(text(), 'Logout')] | //button[contains(text(), 'Logout')] | //form[contains(@action, 'Logout')]")).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Authentication check failed. Current URL: {Driver.Url}");
                Console.WriteLine("Page Source:");
                Console.WriteLine(Driver.PageSource);
                throw;
            }
             var elements = Driver.FindElements(By.XPath("//a[contains(text(), 'Logout')] | //button[contains(text(), 'Logout')] | //form[contains(@action, 'Logout')]"));
            Assert.That(elements.Count, Is.GreaterThan(0), "User should be authenticated but logout element was not found.");
        }

        [When(@"""(.*)"" removes ""(.*)""")]
        public async Task WhenRemoves(string adminUsername, string targetUsername)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            
            var targetUser = await userManager.FindByNameAsync(targetUsername);
            var adminUser = await userManager.FindByNameAsync(adminUsername);

            Assert.That(targetUser, Is.Not.Null, $"Target user {targetUsername} not found");
            Assert.That(adminUser, Is.Not.Null, $"Admin user {adminUsername} not found");

            var result = await userService.DeleteUserAsync(targetUser.Id, adminUser.Id);
            Assert.That(result.Succeeded, Is.True, $"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        [When(@"I navigate to the Dashboard page")]
        public void WhenINavigateToTheDashboardPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Dashboard");
        }

        [Then(@"I should be redirected to the Login page")]
        public void ThenIShouldBeRedirectedToTheLoginPage()
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.Url.Contains("/Account/Login"));
        }

        public void Dispose()
        {
        }
    }
}
