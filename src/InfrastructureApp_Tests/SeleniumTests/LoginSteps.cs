using InfrastructureApp.Models;
using InfrastructureApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using InfrastructureApp_Tests.SeleniumTests.Helpers;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class LoginSteps : SeleniumTestBase, IDisposable
    {
        private readonly ScenarioContext _scenarioContext;

        public LoginSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            await OneTimeSetUp();
            await SetUpDriver();
        }

        [Given(@"a user with username ""(.*)"" and password ""(.*)"" exists")]
        public async Task GivenAUserWithUsernameAndPasswordExists(string username, string password)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var existing = await userManager.FindByNameAsync(username);
            if (existing != null)
            {
                var points = await db.UserPoints.FirstOrDefaultAsync(p => p.UserId == existing.Id);
                if (points != null) db.UserPoints.Remove(points);
                await db.SaveChangesAsync();
                await userManager.DeleteAsync(existing);
            }

            var user = new Users { UserName = username, Email = $"{username}@example.com", EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            db.UserPoints.Add(new UserPoints { UserId = user.Id, CurrentPoints = 0, LifetimePoints = 0 });
            await db.SaveChangesAsync();

            _scenarioContext["CurrentUser"] = user;
        }

        [Given(@"the user's email is confirmed")]
        public async Task GivenTheUsersEmailIsConfirmed()
        {
            var user = (Users)_scenarioContext["CurrentUser"];
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var dbUser = await userManager.FindByIdAsync(user.Id);
            dbUser!.EmailConfirmed = true;
            await userManager.UpdateAsync(dbUser);
        }

        [Given(@"the user's email is not confirmed")]
        public async Task GivenTheUsersEmailIsNotConfirmed()
        {
            var user = (Users)_scenarioContext["CurrentUser"];
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var dbUser = await userManager.FindByIdAsync(user.Id);
            dbUser!.EmailConfirmed = false;
            await userManager.UpdateAsync(dbUser);
        }

        [When(@"I log in with username ""(.*)"" and password ""(.*)""")]
        public void WhenILogInWithUsernameAndPassword(string username, string password)
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");
            
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElement(By.Id("UserName")));

            Driver.FindElement(By.Id("UserName")).Clear();
            Driver.FindElement(By.Id("UserName")).SendKeys(username);
            Driver.FindElement(By.Id("Password")).Clear();
            Driver.FindElement(By.Id("Password")).SendKeys(password);
            Driver.FindElement(By.CssSelector("input[type='submit']")).Click();
        }

        [Then(@"I should be redirected to the home page")]
        public void ThenIShouldBeRedirectedToTheHomePage()
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            try
            {
                wait.Until(d => d.Url == $"{BaseUrl}/" || d.Url == $"{BaseUrl}/Home/Index" || d.Url.EndsWith("/"));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Redirection failed. Current URL: {Driver.Url}");
                throw;
            }
        }

        [Then(@"I should be authenticated")]
        public void ThenIShouldBeAuthenticated()
        {
            var elements = Driver.FindElements(By.XPath("//a[contains(text(), 'Logout')] | //button[contains(text(), 'Logout')] | //form[contains(@action, 'Logout')]"));
            NUnit.Framework.Assert.That(elements.Count, Is.GreaterThan(0), "User should be authenticated but logout element was not found.");
        }

        [Then(@"I should not be authenticated")]
        public void ThenIShouldNotBeAuthenticated()
        {
            var elements = Driver.FindElements(By.XPath("//a[contains(text(), 'Logout')] | //button[contains(text(), 'Logout')] | //form[contains(@action, 'Logout')]"));
            NUnit.Framework.Assert.That(elements.Count, Is.Zero, "User should not be authenticated but logout element was found.");
        }

        [Then(@"I should see an error message ""(.*)""")]
        [Scope(Feature = "Login")]
        public void ThenIShouldSeeAnErrorMessage(string expectedMessage)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(5));
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
                Console.WriteLine($"Error message not found. Expected: {expectedMessage}. Current URL: {Driver.Url}. Page source follows:");
                Console.WriteLine(Driver.PageSource);
                throw;
            }
            
            var bodyText = Driver.FindElement(By.TagName("body")).Text;
            NUnit.Framework.Assert.That(bodyText, Does.Contain(expectedMessage));
        }

        public void Dispose()
        {
            TearDownDriver();
        }

        [AfterTestRun]
        public static async Task AfterTestRun()
        {
            await OneTimeTearDownStatic();
        }
    }
}
