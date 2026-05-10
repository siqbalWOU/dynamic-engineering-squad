using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp_Tests.Helpers;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InfrastructureApp_Tests.SeleniumTests
{
    [TestFixture]
    [Category("Selenium")]
    public class VerifyFixSeleniumTests : SeleniumTestBase
    {
        private int _reportId;

        [SetUp]
        public async Task CreateReport()
        {
            _reportId = await CreateTestReportWithStatus("Cracked footpath near the station", "Approved");
        }

        private async Task<int> CreateTestReportWithStatus(string description, string status)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var report = await ReportIssueTestDataHelper.CreateTestReportAsync(
                db,
                description,
                status,
                "selenium-test-user");
            return report.Id;
        }

        // ── Unauthenticated ──────────────────────────────────────────────────

        [Test]
        public void VerifyButton_WhenNotLoggedIn_ShowsLoginLink()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var verifyLink = wait.Until(d => d.FindElement(By.CssSelector("a.btn-outline-success")));

            Assert.That(verifyLink.Text, Does.Contain("I've verified this is fixed"));
            Assert.That(verifyLink.GetAttribute("href"), Does.Contain("Login"));
        }

        [Test]
        public void VerifyCount_WhenNotLoggedIn_ShowsZero()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var countEl = wait.Until(d => d.FindElement(By.Id("verifyCount")));

            Assert.That(countEl.Text, Is.EqualTo("0"));
        }

        // ── Authenticated ────────────────────────────────────────────────────

        [Test]
        public void VerifyButton_WhenLoggedIn_IsVisible()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("verifyBtn")));

            Assert.That(btn.Displayed, Is.True);
            Assert.That(btn.Text, Does.Contain("I've verified this is fixed"));
        }

        [Test]
        public void VerifyButton_WhenLoggedIn_StartsAsOutline()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("verifyBtn")));

            Assert.That(btn.GetAttribute("class"), Does.Contain("btn-outline-success"));
        }

        [Test]
        public void VerifyButton_AfterClick_CountIncrementsToOne()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("verifyBtn")));
            ScrollAndClick(btn);

            wait.Until(d => d.FindElement(By.Id("verifyCount")).Text == "1");
            Assert.That(Driver.FindElement(By.Id("verifyCount")).Text, Is.EqualTo("1"));
        }

        [Test]
        public void VerifyButton_AfterClick_ButtonBecomesFilledSuccess()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("verifyBtn")));
            ScrollAndClick(btn);

            wait.Until(d =>
            {
                var b = d.FindElement(By.Id("verifyBtn"));
                return b.GetAttribute("class")!.Contains("btn-success") &&
                       !b.GetAttribute("class")!.Contains("btn-outline-success");
            });

            var updatedBtn = Driver.FindElement(By.Id("verifyBtn"));
            Assert.That(updatedBtn.GetAttribute("class"), Does.Contain("btn-success"));
            Assert.That(updatedBtn.GetAttribute("class"), Does.Not.Contain("btn-outline-success"));
        }

        [Test]
        public void VerifyButton_ClickTwice_TogglesVerifyBackToZero()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var btn = wait.Until(d => d.FindElement(By.Id("verifyBtn")));
            ScrollAndClick(btn);

            wait.Until(d => d.FindElement(By.Id("verifyCount")).Text == "1");

            ScrollAndClick(Driver.FindElement(By.Id("verifyBtn")));

            wait.Until(d => d.FindElement(By.Id("verifyCount")).Text == "0");
            Assert.That(Driver.FindElement(By.Id("verifyCount")).Text, Is.EqualTo("0"));
        }

        // ── Verify page ──────────────────────────────────────────────────────

        [Test]
        public async Task VerifyFixesPage_WhenResolvedReportExists_ShowsReport()
        {
            var resolvedId = await CreateTestReportWithStatus("Fixed road near school", "Resolved");

            Driver.Navigate().GoToUrl($"{BaseUrl}/Reports/Verify");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.CssSelector(".card")));

            Assert.That(Driver.PageSource, Does.Contain("Fixed road near school"));
        }
    }
}
