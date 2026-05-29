using InfrastructureApp.Data;
using InfrastructureApp_Tests.Helpers;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InfrastructureApp_Tests.SeleniumTests
{
    // SCRUM-137: Selenium coverage for submitted reports on the logged-in user's private Dashboard.
    [TestFixture]
    [Category("Selenium")]
    public class DashboardSubmittedReportsSeleniumTests : SeleniumTestBase
    {
        private string _ownReportDescription = string.Empty;
        private string _otherReportDescription = string.Empty;

        [SetUp]
        public async Task SeedDashboardSubmittedReports()
        {
            var uniqueSuffix = Guid.NewGuid().ToString("N");
            _ownReportDescription = $"SCRUM-137 own dashboard report {uniqueSuffix}";
            _otherReportDescription = $"SCRUM-137 other dashboard report {uniqueSuffix}";

            using var scope = ServerHost!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var currentUser = await db.Users.FirstAsync(user => user.UserName == "ErinBleu");

            await ReportIssueTestDataHelper.CreateTestReportAsync(
                db,
                _ownReportDescription,
                "Approved",
                currentUser.Id,
                DateTime.UtcNow);

            await ReportIssueTestDataHelper.CreateTestReportAsync(
                db,
                _otherReportDescription,
                "Approved",
                $"scrum-137-other-user-{uniqueSuffix}",
                DateTime.UtcNow,
                userName: $"scrum137other{uniqueSuffix}",
                email: $"scrum137other{uniqueSuffix}@test.local");
        }

        // TEST 1: The logged-in user can open the Dashboard and see their submitted report list.
        [Test]
        public void DashboardSubmittedReports_LoggedInUserSeesOwnSubmittedReport()
        {
            Login();

            Driver.Navigate().GoToUrl($"{BaseUrl}/Dashboard");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.XPath("//*[contains(text(), 'My Submitted Reports')]")));
            wait.Until(d => d.PageSource.Contains(_ownReportDescription));

            Assert.That(Driver.PageSource, Does.Contain("My Submitted Reports"));
            Assert.That(Driver.PageSource, Does.Contain(_ownReportDescription));
            Assert.That(Driver.PageSource, Does.Contain("Approved"));
        }

        // TEST 2: The logged-in user's Dashboard does not show another user's submitted report.
        [Test]
        public void DashboardSubmittedReports_LoggedInUserDoesNotSeeOtherUsersReport()
        {
            Login();

            Driver.Navigate().GoToUrl($"{BaseUrl}/Dashboard");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.XPath("//*[contains(text(), 'My Submitted Reports')]")));

            Assert.That(Driver.PageSource, Does.Contain(_ownReportDescription));
            Assert.That(Driver.PageSource, Does.Not.Contain(_otherReportDescription));
        }
    }
}
