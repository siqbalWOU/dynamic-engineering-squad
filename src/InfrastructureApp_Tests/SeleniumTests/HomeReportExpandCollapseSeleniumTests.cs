using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InfrastructureApp_Tests.SeleniumTests
{
    [TestFixture]
    [Category("Selenium")]
    public class HomeReportExpandCollapseSeleniumTests : SeleniumTestBase
    {
        [SetUp]
        public async Task SeedHomeReportExpandCollapseReports()
        {
            using var scope = ServerHost!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.ReportIssue.AddRange(
                new ReportIssue
                {
                    Description = "Selenium expand collapse pothole report",
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                    UserId = "selenium-expand-user-1"
                },
                new ReportIssue
                {
                    Description = "Selenium expand collapse streetlight report",
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow,
                    UserId = "selenium-expand-user-2"
                });

            await db.SaveChangesAsync();
        }

        // SCRUM-128:
        // TEST 1: Clicking Expand shows the inline report details panel
        [Test]
        public void HomeReportExpandCollapse_ClickExpand_ShowsInlineDetails()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var reportItem = wait.Until(d => d.FindElement(By.CssSelector("#recentReportsList .home-report-item")));
            var expandButton = reportItem.FindElement(By.CssSelector(".home-report-toggle"));
            var detailsPanel = reportItem.FindElement(By.CssSelector(".home-report-details"));

            ScrollAndClick(expandButton);

            wait.Until(_ => detailsPanel.Displayed);

            Assert.That(detailsPanel.Displayed, Is.True);
            Assert.That(expandButton.GetAttribute("aria-expanded"), Is.EqualTo("true"));
            Assert.That(expandButton.Text, Is.EqualTo("Collapse"));
        }

        // SCRUM-128:
        // TEST 2: Clicking Collapse hides the inline report details panel
        [Test]
        public void HomeReportExpandCollapse_ClickCollapse_HidesInlineDetails()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var reportItem = wait.Until(d => d.FindElement(By.CssSelector("#recentReportsList .home-report-item")));
            var expandButton = reportItem.FindElement(By.CssSelector(".home-report-toggle"));
            var detailsPanel = reportItem.FindElement(By.CssSelector(".home-report-details"));

            ScrollAndClick(expandButton);
            wait.Until(_ => detailsPanel.Displayed);

            ScrollAndClick(expandButton);
            wait.Until(_ => !detailsPanel.Displayed);

            Assert.That(detailsPanel.Displayed, Is.False);
            Assert.That(expandButton.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(expandButton.Text, Is.EqualTo("Expand"));
        }

        // SCRUM-128:
        // TEST 3: Expanding a second report collapses the first report
        [Test]
        public void HomeReportExpandCollapse_ExpandingSecondReport_CollapsesFirstReport()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElements(By.CssSelector("#recentReportsList .home-report-item")).Count >= 2);

            var reportItems = Driver.FindElements(By.CssSelector("#recentReportsList .home-report-item"));
            var firstItem = reportItems[0];
            var secondItem = reportItems[1];
            var firstButton = firstItem.FindElement(By.CssSelector(".home-report-toggle"));
            var secondButton = secondItem.FindElement(By.CssSelector(".home-report-toggle"));
            var firstDetails = firstItem.FindElement(By.CssSelector(".home-report-details"));
            var secondDetails = secondItem.FindElement(By.CssSelector(".home-report-details"));

            ScrollAndClick(firstButton);
            wait.Until(_ => firstDetails.Displayed);

            ScrollAndClick(secondButton);
            wait.Until(_ => secondDetails.Displayed && !firstDetails.Displayed);

            Assert.That(firstDetails.Displayed, Is.False);
            Assert.That(firstButton.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(secondDetails.Displayed, Is.True);
            Assert.That(secondButton.GetAttribute("aria-expanded"), Is.EqualTo("true"));
        }
    }
}
