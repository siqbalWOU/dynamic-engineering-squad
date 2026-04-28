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
            var link = Driver.FindElements(By.XPath("//a[contains(text(), 'Forgot Password?')]"));
            Assert.That(link.Count, Is.GreaterThan(0), "Forgot Password? link not found.");
        }

        [When(@"I click the ""Forgot Password"" link")]
        public void WhenIClickTheForgotPasswordLink()
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var link = wait.Until(d => d.FindElement(By.XPath("//a[contains(text(), 'Forgot Password?')]")));
            link.Click();
        }

        [When(@"I enter ""(.*)"" as my email address")]
        public void WhenIEnterAsMyEmailAddress(string email)
        {
            Driver.FindElement(By.Id("Email")).SendKeys(email);
        }

        [When(@"I click ""Send Reset Link""")]
        public void WhenIClickSendResetLink()
        {
            Driver.FindElement(By.XPath("//button[contains(text(), 'Send Reset Link')]")).Click();
        }

        [Then(@"I should see a message ""(.*)""")]
        [Scope(Feature = "Forgot Password")]
        public void ThenIShouldSeeAMessage(string expectedMessage)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(45));

            wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));

            try
            {
                wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains(expectedMessage));
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
            Driver.FindElement(By.Id("Password")).SendKeys(password);
        }

        [When(@"I confirm ""(.*)"" as my new password")]
        public void WhenIConfirmAsMyNewPassword(string password)
        {
            Driver.FindElement(By.Id("ConfirmPassword")).SendKeys(password);
        }

        [When(@"I click ""Reset Password""")]
        public void WhenIClickResetPassword()
        {
            Driver.FindElement(By.XPath("//button[contains(text(), 'Reset Password')]")).Click();
        }

        [When(@"I navigate to the Reset Password page with an invalid token")]
        public void WhenINavigateToTheResetPasswordPageWithAnInvalidToken()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ResetPassword?email=test@example.com&token=invalidtoken");
            Driver.FindElement(By.Id("Password")).SendKeys("ValidPassword123!");
            Driver.FindElement(By.Id("ConfirmPassword")).SendKeys("ValidPassword123!");
        }

        [Then(@"I should see an error message ""(.*)""")]
        [Scope(Feature = "Forgot Password")]
        public void ThenIShouldSeeAnErrorMessage(string expectedMessage)
        {
            // If we are on ResetPassword page and haven't clicked the button, click it
            if (Driver.Url.Contains("ResetPassword") && Driver.FindElements(By.XPath("//button[contains(text(), 'Reset Password')]")).Count > 0)
            {
                var button = Driver.FindElement(By.XPath("//button[contains(text(), 'Reset Password')]"));
                // Need to enter something to satisfy client side validation if any, 
                // but the goal is to trigger server side "Invalid Token"
                button.Click();
            }

            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(45));

            wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));

            try
            {
                // wait.Until returns true when it succeeds. Capture it directly.
                bool messageFound = wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains(expectedMessage));
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
    }
}
