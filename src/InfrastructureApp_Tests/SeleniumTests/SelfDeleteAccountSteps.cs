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
    public class SelfDeleteAccountSteps : SeleniumTestBase
    {
        private readonly ScenarioContext _scenarioContext;

        public SelfDeleteAccountSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [When(@"I log in with username ""(.*)"" and password ""(.*)"" for SelfDelete")]
        public void WhenILogInWithUsernameAndPasswordForSelfDelete(string username, string password)
        {
            Login(username, password);
        }

        [Then(@"I should see a ""Delete Account"" option")]
        public void ThenIShouldSeeADeleteAccountOption()
        {
            var deleteLink = Driver.FindElements(By.XPath("//a[contains(text(), 'Delete Account')] | //button[contains(text(), 'Delete Account')]"));
            Assert.That(deleteLink.Count, Is.GreaterThan(0), "Delete Account option not found on dashboard.");
        }

        [When(@"I click ""Delete Account""")]
        public void WhenIClickDeleteAccount()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var deleteLink = wait.Until(d =>
            {
                var element = d.FindElements(By.CssSelector("[data-testid='dashboard-delete-account']"))
                    .Concat(d.FindElements(By.XPath("//a[contains(text(), 'Delete Account')] | //button[contains(text(), 'Delete Account')]")))
                    .FirstOrDefault();

                return element is { Displayed: true, Enabled: true } ? element : null;
            });

            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "arguments[0].scrollIntoView({ block: 'center', inline: 'nearest' });",
                deleteLink);

            wait.Until(_ => deleteLink.Displayed && deleteLink.Enabled);

            ScrollAndClick(deleteLink);
        }

        [Then(@"I should be on the ""Delete Account"" confirmation page")]
        public void ThenIShouldBeOnTheConfirmationPage()
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            wait.Until(d => d.Url.Contains("/Account/DeleteAccount"));
        }

        [Then(@"I should see a warning that this action is irreversible")]
        public void ThenIShouldSeeAWarningThatThisActionIsIrreversible()
        {
            var bodyText = Driver.FindElement(By.TagName("body")).Text;
            Assert.That(bodyText, Does.Contain("irreversible").IgnoreCase.Or.Contain("warning").IgnoreCase);
        }

        [Then(@"I should see a field to enter my current password")]
        public void ThenIShouldSeeAFieldToEnterMyCurrentPassword()
        {
            var passwordField = Driver.FindElements(By.Id("CurrentPassword"));
            Assert.That(passwordField.Count, Is.GreaterThan(0), "Current password field not found.");
        }

        [When(@"I enter ""(.*)"" as my current password")]
        public void WhenIEnterAsMyCurrentPassword(string password)
        {
            Driver.FindElement(By.Id("CurrentPassword")).SendKeys(password);
        }

        [When(@"I confirm the account deletion")]
        public void WhenIConfirmTheAccountDeletion()
        {
            Driver.FindElement(By.Id("deleteAccountConfirmButton")).Click();
        }

        [Then(@"the user ""(.*)"" should no longer exist in the system")]
        public async Task ThenTheUserShouldNoLongerExistInTheSystem(string username)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var user = await userManager.FindByNameAsync(username);
            Assert.That(user, Is.Null, $"User {username} still exists in the system.");
        }

        [Then(@"I should still be authenticated")]
        public void ThenIShouldStillBeAuthenticated()
        {
            var elements = Driver.FindElements(By.XPath("//a[contains(text(), 'Logout')] | //button[contains(text(), 'Logout')] | //form[contains(@action, 'Logout')]"));
            Assert.That(elements.Count, Is.GreaterThan(0), "User should still be authenticated.");
        }

        [Then(@"I should see an error message ""(.*)""")]
        [Scope(Feature = "Self Delete Account")]
        public void ThenIShouldSeeAnErrorMessage(string expectedMessage)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));
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
                Console.WriteLine($"Error message '{expectedMessage}' not found. Page Source:");
                Console.WriteLine(Driver.PageSource);
                throw;
            }
            var bodyText = Driver.FindElement(By.TagName("body")).Text;
            Assert.That(bodyText, Does.Contain(expectedMessage));
        }

        public void Dispose()
        {
        }
    }
}
