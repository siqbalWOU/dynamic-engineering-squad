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
    public class VoteSeleniumTests : SeleniumTestBase
    {
        private int _reportId;

        [SetUp]
        public async Task CreateReport()
        {
            _reportId = await CreateTestReport("Cracked sidewalk near the park");
        }

        private async Task<int> CreateTestReport(string description)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var report = new ReportIssue
            {
                Description = description,
                Status = "Approved",
                UserId = "selenium-test-user",
                CreatedAt = DateTime.UtcNow
            };

            db.ReportIssue.Add(report);
            await db.SaveChangesAsync();
            return report.Id;
        }

        // ── Unauthenticated ──────────────────────────────────────────────────

        [Test]
        public void VoteButton_WhenNotLoggedIn_ShowsLoginLink()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var voteArea = wait.Until(d => d.FindElement(By.CssSelector("a.btn-outline-primary")));

            Assert.That(voteArea.Text, Does.Contain("I've seen this too"));
            Assert.That(voteArea.GetAttribute("href"), Does.Contain("Login"));
        }

        [Test]
        public void VoteCount_WhenNotLoggedIn_ShowsZero()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var countEl = wait.Until(d => d.FindElement(By.Id("voteCount")));

            Assert.That(countEl.Text, Is.EqualTo("0"));
        }

        // ── Authenticated ────────────────────────────────────────────────────

        [Test]
        public void VoteButton_WhenLoggedIn_IsVisible()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("voteBtn")));

            Assert.That(btn.Displayed, Is.True);
            Assert.That(btn.Text, Does.Contain("I've seen this too"));
        }

        [Test]
        public void VoteButton_WhenLoggedIn_StartsAsOutline()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("voteBtn")));

            Assert.That(btn.GetAttribute("class"), Does.Contain("btn-outline-primary"));
        }

        [Test]
        public void VoteButton_AfterClick_VoteCountIncrementsToOne()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("voteBtn")));
            ScrollAndClick(btn);

            wait.Until(d => d.FindElement(By.Id("voteCount")).Text == "1");
            Assert.That(Driver.FindElement(By.Id("voteCount")).Text, Is.EqualTo("1"));
        }

        [Test]
        public void VoteButton_AfterClick_ButtonBecomesFilledPrimary()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("voteBtn")));
            ScrollAndClick(btn);

            wait.Until(d =>
            {
                var b = d.FindElement(By.Id("voteBtn"));
                return b.GetAttribute("class")!.Contains("btn-primary") &&
                       !b.GetAttribute("class")!.Contains("btn-outline-primary");
            });

            var updatedBtn = Driver.FindElement(By.Id("voteBtn"));
            Assert.That(updatedBtn.GetAttribute("class"), Does.Contain("btn-primary"));
            Assert.That(updatedBtn.GetAttribute("class"), Does.Not.Contain("btn-outline-primary"));
        }

        [Test]
        public void VoteButton_ClickTwice_TogglesVoteBackToZero()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("voteBtn")));
            ScrollAndClick(btn);

            wait.Until(d => d.FindElement(By.Id("voteCount")).Text == "1");

            ScrollAndClick(Driver.FindElement(By.Id("voteBtn")));

            wait.Until(d => d.FindElement(By.Id("voteCount")).Text == "0");
            Assert.That(Driver.FindElement(By.Id("voteCount")).Text, Is.EqualTo("0"));
        }

        // ── Latest Reports modal ─────────────────────────────────────────────

        [Test]
        public void LatestReportsPage_ContainsVoteButton()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Reports/Latest");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.Id("latestReportsList")));

            var pageSource = Driver.PageSource;
            Assert.That(pageSource, Does.Contain("I've seen this too"));
        }
    }
}
