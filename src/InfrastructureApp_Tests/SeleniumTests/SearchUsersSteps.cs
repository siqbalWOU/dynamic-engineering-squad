using InfrastructureApp.Models;
using InfrastructureApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using NUnit.Framework;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    [Scope(Feature = "Search Users")]
    public class SearchUsersSteps : SeleniumTestBase
    {
        [Given(@"^a user with username ""([^""]*)"" exists$")]
        public async Task GivenAUserWithUsernameExists(string username)
        {
            await CreateTestUser(username, "Password123!");
        }

        [Then(@"I should see a search input field")]
        public void ThenIShouldSeeASearchInputField()
        {
            EnsureOnAdminPage();
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var searchInput = wait.Until(d => d.FindElement(By.Id("userSearchInput")));
            Assert.That(searchInput.Displayed, Is.True);
        }

        [When(@"I enter ""(.*)"" into the search field")]
        public void WhenIEnterIntoTheSearchField(string searchTerm)
        {
            EnsureOnAdminPage();
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            
            try 
            {
                var searchInput = wait.Until(d => {
                    var el = d.FindElement(By.Id("userSearchInput"));
                    return el.Displayed ? el : null;
                });
                
                // Avoid Clear() if it's already empty to prevent unwanted reloads
                if (!string.IsNullOrEmpty(searchInput.GetAttribute("value")))
                {
                    searchInput.Clear();
                    // If Clear() triggered a reload, wait for it
                    try {
                        wait.Until(ExpectedConditions.StalenessOf(searchInput));
                        searchInput = wait.Until(d => d.FindElement(By.Id("userSearchInput")));
                    } catch (WebDriverTimeoutException) {
                        // No reload happened, proceed
                    }
                }
                
                searchInput.SendKeys(searchTerm);
                
                // Wait for debounce (500ms) and page reload
                wait.Until(d => d.Url.Contains("searchTerm=") || d.PageSource.Contains("No users found matching"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search failed. URL: {Driver.Url}");
                Console.WriteLine($"Page source snippet: {Driver.PageSource.Substring(0, Math.Min(Driver.PageSource.Length, 1000))}");
                throw;
            }
        }

        private void EnsureOnAdminPage()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
            
            if (!Driver.Url.Contains("/Account/Admin"))
            {
                Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Admin");
            }
            
            // Handle unauthorized or session loss
            if (Driver.Url.Contains("/Account/Login"))
            {
                Console.WriteLine("Redirected to Login. Attempting emergency re-login as adminuser.");
                Login("adminuser", "AdminPassword123!");
                Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Admin");
            }
            
            wait.Until(d => d.Url.Contains("/Account/Admin"));
        }

        [Then(@"I should see ""(.*)"" in the user list")]
        public void ThenIShouldSeeInTheUserList(string username)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.TagName("table")));
            var userRows = Driver.FindElements(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]"));
            Assert.That(userRows.Count, Is.GreaterThan(0), $"User {username} not found in user list.");
        }

        [Then(@"I should not see ""(.*)"" in the user list")]
        public void ThenIShouldNotSeeInTheUserList(string username)
        {
            var userRows = Driver.FindElements(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]"));
            Assert.That(userRows.Count, Is.EqualTo(0), $"User {username} found in user list but should not be.");
        }

        [Then(@"I should see an empty state message ""(.*)""")]
        public void ThenIShouldSeeAnEmptyStateMessage(string expectedMessage)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var emptyMessage = wait.Until(d => d.FindElement(By.CssSelector(".alert-info")));
            Assert.That(emptyMessage.Text, Does.Contain(expectedMessage));
        }

        [When(@"I clear the search field")]
        public void WhenIClearTheSearchField()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var clearBtn = wait.Until(d => d.FindElement(By.Id("clearSearchBtn")));
            clearBtn.Click();
            
            // Wait for reset (URL should no longer contain searchTerm)
            wait.Until(d => !d.Url.Contains("searchTerm="));
        }
    }
}
