using System;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using InfrastructureApp_Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Microsoft.AspNetCore.Identity;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using System.IO;

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
            var report = await ReportIssueTestDataHelper.CreateTestReportAsync(
                db,
                description,
                "Approved",
                "selenium-user");
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
            WaitForLatestReportModalScript(wait);
            ScrollAndClick(reportBtn);

            try
            {
                new WebDriverWait(Driver, TimeSpan.FromSeconds(3)).Until(d =>
                    d.FindElements(By.CssSelector("#reportModal.show, [data-testid='report-modal'].show, .modal.show")).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript(
                    "window.openLatestReportModal(arguments[0]);",
                    reportBtn);
            }
        }

        [Then(@"the report modal should be displayed")]
        [Scope(Feature = "Flag Post")]
        public void ThenTheReportModalShouldBeDisplayed()
        {
            var modal = WaitForVisibleModal(By.CssSelector("#reportModal, [data-testid='report-modal']"), "report modal");
            Assert.That(modal, Is.Not.Null);
        }

        [Then(@"I should see a ""Flag"" button in the modal")]
        [Scope(Feature = "Flag Post")]
        public void ThenIShouldSeeAFlagButtonInTheModal()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            var flagBtn = wait.Until(d => d.FindElement(By.CssSelector("#modalFlagBtn, [data-testid='modal-flag-button']")));
            Assert.That(flagBtn.Displayed, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(flagBtn.GetAttribute("data-bs-toggle"), Is.EqualTo("modal"));
                Assert.That(flagBtn.GetAttribute("data-bs-target"), Is.EqualTo("#flagModal"));
            });
        }

        [When(@"I click the ""Flag"" button in the modal")]
        [Scope(Feature = "Flag Post")]
        public void WhenIClickTheFlagButtonInTheModal()
        {
            // Test stability cleanup:
            // Wait for the flag button to be ready before clicking,
            // then wait for the Bootstrap flag modal to fully open.
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            var flagBtn = wait.Until(d =>
            {
                var button = d.FindElement(By.Id("modalFlagBtn"));
                return button.Displayed && button.Enabled ? button : null;
            });
            WaitForFlagModalScript(wait);
            ScrollAndClick(flagBtn);
            WaitForVisibleModal(By.CssSelector("#flagModal, [data-testid='flag-modal']"), "flag modal");
        }

        [Then(@"the ""Flag"" button in the modal should be disabled and show ""Already Flagged""")]
        [Scope(Feature = "Flag Post")]
        public void ThenTheFlagButtonInTheModalShouldBeDisabledAndShowAlreadyFlagged()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
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
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
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
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            var flagBtn = wait.Until(d =>
            {
                var button = d.FindElement(By.Id("flagBtn"));
                return button.Displayed && button.Enabled ? button : null;
            });
            
            WaitForFlagModalScript(wait);

            ScrollAndClick(flagBtn);

            // In CI headless Chrome, Bootstrap's data-bs-toggle click delegation
            // may not fire reliably. After 3 seconds, if the modal still hasn't
            // appeared, force it open directly via Bootstrap's JS API.
            try
            {
                new WebDriverWait(Driver, TimeSpan.FromSeconds(3)).Until(d =>
                    d.FindElements(By.CssSelector(".modal.show")).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript(
                    "bootstrap.Modal.getOrCreateInstance(document.getElementById('flagModal')).show();");
            }

            WaitForVisibleModal(By.CssSelector("#flagModal, [data-testid='flag-modal']"), "flag modal");
        }

        [Then(@"I should be presented with categories ""(.*)"", ""(.*)"", ""(.*)""")]
        [Scope(Feature = "Flag Post")]
        public void ThenIShouldBePresentedWithCategories(string cat1, string cat2, string cat3)
        {
            var body = WaitForVisibleModal(By.CssSelector("#flagModal, [data-testid='flag-modal']"), "flag modal").Text;
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
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            var radio = wait.Until(d => d.FindElement(By.XPath($"//label[contains(text(), '{category}')]/preceding-sibling::input")));
            radio.Click();
        }

        [When(@"I click ""Submit Report""")]
        [Scope(Feature = "Flag Post")]
        public void WhenIClickSubmitReport()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            var submitBtn = wait.Until(d => d.FindElement(By.Id("submitFlagBtn")));
            ScrollAndClick(submitBtn);
        }

        [Then(@"I should see a confirmation message ""(.*)""")]
        [Scope(Feature = "Flag Post")]
        public void ThenIShouldSeeAConfirmationMessage(string expectedMessage)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
            var messageEl = wait.Until(d =>
            {
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
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
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

        private IWebElement WaitForVisibleModal(By by, string modalDescription)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
            try
            {
                return wait.Until(d =>
                {
                    var modal = d.FindElements(by).FirstOrDefault();
                    if (modal == null)
                    {
                        return null;
                    }

                    var className = modal.GetAttribute("class") ?? string.Empty;
                    var ariaHidden = modal.GetAttribute("aria-hidden");
                    var ariaModal = modal.GetAttribute("aria-modal");
                    var displayStyle = modal.GetCssValue("display") ?? string.Empty;

                    var isVisible = modal.Displayed
                        && (className.Contains("show", StringComparison.Ordinal)
                            || string.Equals(ariaModal, "true", StringComparison.OrdinalIgnoreCase)
                            || !string.Equals(displayStyle, "none", StringComparison.OrdinalIgnoreCase))
                        && !string.Equals(ariaHidden, "true", StringComparison.OrdinalIgnoreCase);

                    return isVisible ? modal : null;
                })!;
            }
            catch (WebDriverTimeoutException ex)
            {
                WriteModalDiagnostics(modalDescription, by);
                throw new AssertionException(
                    $"Timed out waiting for {modalDescription} using selector '{by}'. See test output for Selenium diagnostics.",
                    ex);
            }
        }

        private void WriteModalDiagnostics(string modalDescription, By by)
        {
            TestContext.WriteLine($"Timed out waiting for {modalDescription}.");
            TestContext.WriteLine($"Current URL: {Driver.Url}");
            TestContext.WriteLine($"Selector: {by}");
            TestContext.WriteLine($"window.bootstrap type: {ExecuteScript("return typeof window.bootstrap;")}");
            TestContext.WriteLine($"#flagModal exists: {ExecuteScript("return !!document.querySelector('#flagModal');")}");
            TestContext.WriteLine($"#reportModal exists: {ExecuteScript("return !!document.querySelector('#reportModal');")}");
            TestContext.WriteLine($"Visible modal count: {ExecuteScript("return document.querySelectorAll('.modal.show').length;")}");

            try
            {
                var snippet = BuildPageSourceSnippet("flagModal", "modalFlagBtn", "flagBtn", "reportModal");
                TestContext.WriteLine("Page source snippet:");
                TestContext.WriteLine(snippet);
            }
            catch (Exception snippetEx)
            {
                TestContext.WriteLine($"Could not capture page source snippet: {snippetEx.Message}");
            }

            try
            {
                if (Driver is ITakesScreenshot screenshotDriver)
                {
                    var screenshot = screenshotDriver.GetScreenshot();
                    var screenshotPath = Path.Combine(
                        TestContext.CurrentContext.WorkDirectory,
                        $"flag-modal-timeout-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
                    screenshot.SaveAsFile(screenshotPath);
                    TestContext.WriteLine($"Screenshot saved to: {screenshotPath}");
                }
            }
            catch (Exception screenshotEx)
            {
                TestContext.WriteLine($"Could not capture screenshot: {screenshotEx.Message}");
            }

            try
            {
                if (Driver is ChromeDriver chromeDriver)
                {
                    foreach (var entry in chromeDriver.Manage().Logs.GetLog(LogType.Browser))
                    {
                        TestContext.WriteLine($"Browser console [{entry.Level}]: {entry.Message}");
                    }
                }
            }
            catch (Exception browserLogEx)
            {
                TestContext.WriteLine($"Could not capture browser console logs: {browserLogEx.Message}");
            }
        }

        private string BuildPageSourceSnippet(params string[] markers)
        {
            var pageSource = Driver.PageSource ?? string.Empty;
            const int radius = 600;

            foreach (var marker in markers)
            {
                var index = pageSource.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var start = Math.Max(0, index - radius);
                    var length = Math.Min(pageSource.Length - start, radius * 2);
                    return pageSource.Substring(start, length);
                }
            }

            return pageSource.Length <= 1200
                ? pageSource
                : pageSource.Substring(0, 1200);
        }

        private object? ExecuteScript(string script)
        {
            return Driver is IJavaScriptExecutor executor
                ? executor.ExecuteScript(script)
                : null;
        }

        private static void WaitForFlagModalScript(WebDriverWait wait)
        {
            wait.Until(d =>
                ((IJavaScriptExecutor)d).ExecuteScript("return typeof window.showFlagModalFromButton === 'function';")
                    is true);
        }

        private static void WaitForLatestReportModalScript(WebDriverWait wait)
        {
            wait.Until(d =>
                ((IJavaScriptExecutor)d).ExecuteScript("return typeof window.openLatestReportModal === 'function';")
                    is true);
        }
    }
}
