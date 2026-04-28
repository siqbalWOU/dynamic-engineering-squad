using System;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Microsoft.AspNetCore.Identity;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class FlagReportSteps : SeleniumTestBase
    {
        private int _reportId;

        [Given(@"a report exists with description ""(.*)""")]
        [Scope(Feature = "Flag Post")]
        public async Task GivenAReportExistsWithDescription(string description)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var report = new ReportIssue
            {
                Description = description,
                Status = "Approved",
                UserId = "selenium-user",
                CreatedAt = DateTime.UtcNow
            };

            db.ReportIssue.Add(report);
            await db.SaveChangesAsync();
            _reportId = report.Id;
        }

        [Given(@"I am logged in as ""(.*)""")]
        [Scope(Feature = "Flag Post")]
        public async Task GivenIAmLoggedInAs(string username)
        {
            await CreateTestUser(username, "Password123!");
            Login(username, "Password123!");
        }

        [Given(@"I navigate to that report's details page")]
        [When(@"I navigate to that report's details page")]
        [Scope(Feature = "Flag Post")]
        public void WhenINavigateToThatReportsDetailsPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");
        }

        [When(@"I navigate to the Latest Reports page")]
        [Scope(Feature = "Flag Post")]
        public void WhenINavigateToTheLatestReportsPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Reports/Latest");
        }

        [When(@"I click on the report with description ""(.*)""")]
        [Scope(Feature = "Flag Post")]
        public void WhenIClickOnTheReportWithDescription(string description)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
            var reportBtn = wait.Until(d =>
                d.FindElements(By.CssSelector("[data-testid='latest-report-item'], button.report-item"))
                    .FirstOrDefault(button => button.Text.Contains(description, StringComparison.Ordinal)));

            Assert.That(reportBtn, Is.Not.Null, $"Could not find report item with description '{description}'.");
            ScrollAndClick(reportBtn);
        }

        [Then(@"the report modal should be displayed")]
        [Scope(Feature = "Flag Post")]
        public void ThenTheReportModalShouldBeDisplayed()
        {
            var modal = WaitForVisibleModal(By.CssSelector("[data-testid='report-modal'], #reportModal"));
            Assert.That(modal, Is.Not.Null);
        }

        [Then(@"I should see a ""Flag"" button in the modal")]
        [Scope(Feature = "Flag Post")]
        public void ThenIShouldSeeAFlagButtonInTheModal()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var flagBtn = wait.Until(d => d.FindElement(By.CssSelector("[data-testid='modal-flag-button'], #modalFlagBtn")));
            Assert.That(flagBtn.Displayed, Is.True);
        }

        [When(@"I click the ""Flag"" button in the modal")]
        [Scope(Feature = "Flag Post")]
        public void WhenIClickTheFlagButtonInTheModal()
        {
            // Test stability cleanup:
            // Wait for the flag button to be ready before clicking,
            // then wait for the Bootstrap flag modal to fully open.
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var flagBtn = wait.Until(d =>
            {
                var button = d.FindElement(By.Id("modalFlagBtn"));
                return button.Displayed && button.Enabled ? button : null;
            });
            ScrollAndClick(flagBtn);
            
            wait.Until(d => {
                var modal = d.FindElement(By.Id("flagModal"));
                var modalClass = modal.GetAttribute("class") ?? string.Empty;
                return modal.Displayed && modalClass.Contains("show");
            });
        }

        [Then(@"the ""Flag"" button in the modal should be disabled and show ""Already Flagged""")]
        [Scope(Feature = "Flag Post")]
        public void ThenTheFlagButtonInTheModalShouldBeDisabledAndShowAlreadyFlagged()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var flagBtn = wait.Until(d => d.FindElement(By.Id("modalFlagBtn")));
            Assert.Multiple(() =>
            {
                Assert.That(flagBtn.Enabled, Is.False);
                Assert.That(flagBtn.GetAttribute("innerText"), Does.Contain("Already Flagged"));
            });
        }

        [Then(@"I should see a ""Flag"" icon")]
        [Scope(Feature = "Flag Post")]
        public void ThenIShouldSeeAFlagIcon()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var flagBtn = wait.Until(d => d.FindElement(By.Id("flagBtn")));
            Assert.That(flagBtn.Displayed, Is.True);
        }

        [When(@"I click the ""Flag"" icon")]
        [Given(@"I have clicked the ""Flag"" icon")]
        [Scope(Feature = "Flag Post")]
        public void WhenIClickTheFlagIcon()
        {
            // Test stability cleanup:
            // Wait for the flag button to be ready before clicking,
            // then wait for the Bootstrap flag modal to fully open.
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var flagBtn = wait.Until(d =>
            {
                var button = d.FindElement(By.Id("flagBtn"));
                return button.Displayed && button.Enabled ? button : null;
            });
            ScrollAndClick(flagBtn);
            
            wait.Until(d => {
                var modal = d.FindElement(By.Id("flagModal"));
                var modalClass = modal.GetAttribute("class") ?? string.Empty;
                return modal.Displayed && modalClass.Contains("show");
            });

            ScrollAndClick(flagBtn);

            WaitForVisibleModal(By.Id("flagModal"));
        }

        [Then(@"I should be presented with categories ""(.*)"", ""(.*)"", ""(.*)""")]
        [Scope(Feature = "Flag Post")]
        public void ThenIShouldBePresentedWithCategories(string cat1, string cat2, string cat3)
        {
            var body = WaitForVisibleModal(By.Id("flagModal")).Text;
            Assert.Multiple(() =>
            {
                Assert.That(body, Does.Contain(cat1));
                Assert.That(body, Does.Contain(cat2));
                Assert.That(body, Does.Contain(cat3));
            });
        }

        [When(@"I select category ""(.*)""")]
        [Scope(Feature = "Flag Post")]
        public void WhenISelectCategory(string category)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var radio = wait.Until(d => d.FindElement(By.XPath($"//label[contains(text(), '{category}')]/preceding-sibling::input")));
            radio.Click();
        }

        [When(@"I click ""Submit Report""")]
        [Scope(Feature = "Flag Post")]
        public void WhenIClickSubmitReport()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var submitBtn = wait.Until(d => d.FindElement(By.Id("submitFlagBtn")));
            ScrollAndClick(submitBtn);
        }

        [Then(@"I should see a confirmation message ""(.*)""")]
        [Scope(Feature = "Flag Post")]
        public void ThenIShouldSeeAConfirmationMessage(string expectedMessage)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
            var messageEl = wait.Until(d => {
                var el = d.FindElement(By.Id("flagMessage"));
                return el.Displayed && !string.IsNullOrEmpty(el.Text) && el.Text.Contains(expectedMessage) ? el : null;
            });
            Assert.That(messageEl, Is.Not.Null);
        }

        [Then(@"the reporting interface should close")]
        [Scope(Feature = "Flag Post")]
        public void ThenTheReportingInterfaceShouldClose()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
            wait.Until(d =>
            {
                var modal = d.FindElement(By.Id("flagModal"));
                return !modal.Displayed || !modal.GetAttribute("class").Contains("show", StringComparison.Ordinal);
            });

            Assert.That(Driver.FindElement(By.Id("flagModal")).GetAttribute("class"), Does.Not.Contain("show"));
        }

        [Then(@"the ""Flag"" icon should be disabled and show ""Already Flagged""")]
        [Scope(Feature = "Flag Post")]
        public void ThenTheFlagIconShouldBeDisabledAndShowAlreadyFlagged()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var flagBtn = wait.Until(d => d.FindElement(By.Id("flagBtn")));
            Assert.Multiple(() =>
            {
                Assert.That(flagBtn.Enabled, Is.False);
                Assert.That(flagBtn.GetAttribute("innerText"), Does.Contain("Already Flagged"));
            });
        }

        [Given(@"I have already flagged that report with category ""(.*)""")]
        [Scope(Feature = "Flag Post")]
        public async Task GivenIHaveAlreadyFlaggedThatReportWithCategory(string category)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await userManager.FindByNameAsync("testuser");

            db.ReportFlags.Add(new ReportFlag
            {
                ReportIssueId = _reportId,
                UserId = user!.Id,
                Category = category,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            // Refresh the page to reflect the DB change
            Driver.Navigate().Refresh();
        }

        private IWebElement WaitForVisibleModal(By by)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

            return wait.Until(d =>
            {
                var modal = d.FindElement(by);
                var className = modal.GetAttribute("class") ?? string.Empty;
                var ariaHidden = modal.GetAttribute("aria-hidden");
                return modal.Displayed && className.Contains("show", StringComparison.Ordinal) && ariaHidden != "true"
                    ? modal
                    : null;
            })!;
        }
    }
}
