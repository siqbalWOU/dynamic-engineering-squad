using InfrastructureApp.Data;
using InfrastructureApp_Tests.Helpers;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InfrastructureApp_Tests.SeleniumTests
{
    // SCRUM-157: Selenium coverage for Latest Reports pagination.
    [TestFixture]
    [Category("Selenium")]
    public class LatestReportsPaginationSeleniumTests : SeleniumTestBase
    {
        // TEST 1: Latest Reports shows pagination controls when enough reports exist.
        [Test]
        public async Task LatestReports_WhenEnoughReportsExist_ShowsPaginationControls()
        {
            // Arrange: create enough uniquely named reports to force two pages.
            var prefix = BuildUniquePrefix();
            await SeedLatestReports(prefix, count: 12);

            // Act: load Latest Reports filtered to this test data.
            Driver.Navigate().GoToUrl($"{BaseUrl}/Reports/Latest?query={Uri.EscapeDataString(prefix)}");

            // Assert: the user can see the pagination controls below the list.
            var pagination = WaitForVisible(By.CssSelector("nav[aria-label='Latest Reports pagination']"));
            Assert.That(pagination.Displayed, Is.True);
            Assert.That(pagination.Text, Does.Contain("Previous"));
            Assert.That(pagination.Text, Does.Contain("Next"));
        }

        // TEST 2: Clicking Next changes the visible report set.
        [Test]
        public async Task LatestReports_ClickingNext_ChangesVisibleReportSet()
        {
            // Arrange: seed 12 reports so page 2 contains the two oldest matching reports.
            var prefix = BuildUniquePrefix();
            await SeedLatestReports(prefix, count: 12);
            Driver.Navigate().GoToUrl($"{BaseUrl}/Reports/Latest?query={Uri.EscapeDataString(prefix)}");

            // Act: use the same Next control a user clicks.
            ClickNextPage();

            // Assert: page 2 shows the next set and no longer shows the newest first-page report.
            Assert.That(Driver.PageSource, Does.Contain($"{prefix} 02"));
            Assert.That(Driver.PageSource, Does.Contain($"{prefix} 01"));
            Assert.That(Driver.PageSource, Does.Not.Contain($"{prefix} 12"));
        }

        // TEST 3: The second page becomes the active/current page.
        [Test]
        public async Task LatestReports_ClickingNext_MarksSecondPageAsCurrent()
        {
            // Arrange: seed enough reports for page navigation.
            var prefix = BuildUniquePrefix();
            await SeedLatestReports(prefix, count: 12);
            Driver.Navigate().GoToUrl($"{BaseUrl}/Reports/Latest?query={Uri.EscapeDataString(prefix)}");

            // Act: navigate to page 2.
            ClickNextPage();

            // Assert: the active page indicator moves to page 2.
            var currentPage = WaitForVisible(By.CssSelector("nav[aria-label='Latest Reports pagination'] .page-item.active .page-link[aria-current='page']"));
            Assert.That(currentPage.Text.Trim(), Is.EqualTo("2"));
        }

        // TEST 4: A report can still be opened after navigating to another page.
        [Test]
        public async Task LatestReports_AfterNavigatingPages_ReportModalStillOpens()
        {
            // Arrange: navigate to page 2 before opening a report.
            var prefix = BuildUniquePrefix();
            await SeedLatestReports(prefix, count: 12);
            Driver.Navigate().GoToUrl($"{BaseUrl}/Reports/Latest?query={Uri.EscapeDataString(prefix)}");
            ClickNextPage();

            // Act: click a report item rendered on the paginated page.
            var reportItem = WaitForClickable(By.CssSelector("[data-testid='latest-report-item']"));
            ScrollAndClick(reportItem);

            // Assert: the existing modal behavior still works after pagination.
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var modal = wait.Until(driver =>
            {
                var element = driver.FindElement(By.Id("reportModal"));
                return element.GetAttribute("class")!.Contains("show") ? element : null;
            });

            Assert.That(modal.Displayed, Is.True);
            Assert.That(Driver.FindElement(By.Id("modalDescription")).Text, Does.Contain(prefix));
        }

        private static string BuildUniquePrefix()
        {
            return "SCRUM157 Selenium " + Guid.NewGuid().ToString("N")[..8];
        }

        private static async Task SeedLatestReports(string descriptionPrefix, int count)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Future dates keep this test data newer than shared Selenium seed data.
            var startDate = DateTime.UtcNow.AddDays(30);
            var userId = $"{descriptionPrefix.Replace(" ", "-").ToLowerInvariant()}-user";

            for (var i = 1; i <= count; i++)
            {
                await ReportIssueTestDataHelper.CreateTestReportAsync(
                    db,
                    $"{descriptionPrefix} {i:00}",
                    "Approved",
                    userId,
                    createdAt: startDate.AddMinutes(i),
                    latitude: 44.85m,
                    longitude: -123.23m);
            }
        }

        private void ClickNextPage()
        {
            // Click through the rendered pagination control instead of navigating directly.
            var nextLink = WaitForClickable(By.XPath("//nav[@aria-label='Latest Reports pagination']//a[normalize-space()='Next']"));
            ScrollAndClick(nextLink);

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => driver.Url.Contains("page=2"));
        }
    }
}
