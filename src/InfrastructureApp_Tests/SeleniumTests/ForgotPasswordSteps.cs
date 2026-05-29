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
using System.Net;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class ForgotPasswordSteps : SeleniumTestBase
    {
        private readonly ScenarioContext _scenarioContext;

        public ForgotPasswordSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [When(@"I navigate to the Login page")]
        public void WhenINavigateToTheLoginPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");
        }

        [Then(@"I should see a ""Forgot Password"" link")]
        public void ThenIShouldSeeAForgotPasswordLink()
        {
            var link = Driver.FindElements(By.CssSelector("[data-testid='forgot-password-link']"));
            Assert.That(link.Count, Is.GreaterThan(0), "Forgot Password? link not found.");
        }

        [When(@"I click the ""Forgot Password"" link")]
        public void WhenIClickTheForgotPasswordLink()
        {
            var link = WaitForClickable(By.CssSelector("[data-testid='forgot-password-link']"));
            link.Click();
        }

        [When(@"I enter ""(.*)"" as my email address")]
        public void WhenIEnterAsMyEmailAddress(string email)
        {
            var emailInput = WaitForVisible(By.CssSelector("[data-testid='forgot-password-email']"));
            emailInput.Clear();
            emailInput.SendKeys(email);
        }

        [When(@"I click ""Send Reset Link""")]
        public void WhenIClickSendResetLink()
        {
            WaitForClickable(By.CssSelector("[data-testid='forgot-password-submit']")).Click();
        }

        [Then(@"I should see a message ""(.*)""")]
        [Scope(Feature = "Forgot Password")]
        public void ThenIShouldSeeAMessage(string expectedMessage)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(45));

            wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException), typeof(WebDriverException));

            try
            {
                wait.Until(d => GetPageBodyText().Contains(expectedMessage, StringComparison.Ordinal));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Message '{expectedMessage}' not found. Page Source:");
                Console.WriteLine(Driver.PageSource);
                throw;
            }
        }

        [Then(@"a password reset email should be sent to ""(.*)""")]
        public async Task ThenAPasswordResetEmailShouldBeSentTo(string email)
        {

        }

        [Given(@"a valid password reset token for user ""(.*)""")]
        public async Task GivenAValidPasswordResetTokenForUser(string username)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var user = await userManager.FindByNameAsync(username);
            Assert.That(user, Is.Not.Null);

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            _scenarioContext["ResetToken"] = token;
            _scenarioContext["UserId"] = user.Id;
            _scenarioContext["Email"] = user.Email;
        }

        [When(@"I navigate to the Reset Password page with the valid token")]
        public void WhenINavigateToTheResetPasswordPageWithTheValidToken()
        {
            var email = _scenarioContext["Email"].ToString();
            var token = _scenarioContext["ResetToken"].ToString();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ResetPassword?email={WebUtility.UrlEncode(email)}&token={WebUtility.UrlEncode(token)}");
        }

        [When(@"I enter ""(.*)"" as my new password")]
        public void WhenIEnterAsMyNewPassword(string password)
        {
            var passwordInput = WaitForVisible(By.CssSelector("[data-testid='reset-password']"));
            passwordInput.Clear();
            passwordInput.SendKeys(password);
        }

        [When(@"I confirm ""(.*)"" as my new password")]
        public void WhenIConfirmAsMyNewPassword(string password)
        {
            var confirmPasswordInput = WaitForVisible(By.CssSelector("[data-testid='reset-confirm-password']"));
            confirmPasswordInput.Clear();
            confirmPasswordInput.SendKeys(password);
        }

        [When(@"I click ""Reset Password""")]
        public void WhenIClickResetPassword()
        {
            WaitForClickable(By.CssSelector("[data-testid='reset-password-submit']")).Click();
        }

        [When(@"I navigate to the Reset Password page with an invalid token")]
        public void WhenINavigateToTheResetPasswordPageWithAnInvalidToken()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ResetPassword?email=test@example.com&token=invalidtoken");
            WaitForVisible(By.CssSelector("[data-testid='reset-password']")).SendKeys("ValidPassword123!");
            WaitForVisible(By.CssSelector("[data-testid='reset-confirm-password']")).SendKeys("ValidPassword123!");
        }

        [Then(@"I should see an error message ""(.*)""")]
        [Scope(Feature = "Forgot Password")]
        public void ThenIShouldSeeAnErrorMessage(string expectedMessage)
        {
            // If we are on ResetPassword page and haven't clicked the button, click it
            if (Driver.Url.Contains("ResetPassword") && Driver.FindElements(By.CssSelector("[data-testid='reset-password-submit']")).Count > 0)
            {
                var button = WaitForClickable(By.CssSelector("[data-testid='reset-password-submit']"));
                // Need to enter something to satisfy client side validation if any, 
                // but the goal is to trigger server side "Invalid Token"
                button.Click();
            }

            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(45));

            wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException), typeof(WebDriverException));

            try
            {
                bool messageFound = wait.Until(d => GetPageBodyText().Contains(expectedMessage, StringComparison.Ordinal));
                Assert.That(messageFound, Is.True, $"Error message '{expectedMessage}' was not found.");
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Error message '{expectedMessage}' not found. Current URL: {Driver.Url}");
                Console.WriteLine("Page Source:");
                Console.WriteLine(Driver.PageSource);
                throw;
            }
        }

        private string GetPageBodyText()
        {
            return (string)((IJavaScriptExecutor)Driver).ExecuteScript("return document.body ? document.body.innerText : '';")!;
        }
    }
}
