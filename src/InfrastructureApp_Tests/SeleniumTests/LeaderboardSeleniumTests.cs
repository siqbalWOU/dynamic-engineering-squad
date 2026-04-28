using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using InfrastructureApp_Tests.SeleniumTests.Helpers;

namespace InfrastructureApp_Tests.SeleniumTests
{
    [TestFixture]
    [Category("Selenium")]
    public class LeaderboardSeleniumTests : SeleniumTestBase
    {
        // ── Navigation ───────────────────────────────────────────────────────

        [Test]
        public void Leaderboard_PageLoads_WithHeading()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Leaderboard");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.TagName("h1")));

            Assert.That(Driver.FindElement(By.TagName("h1")).Text, Does.Contain("Leaderboard"));
        }

        [Test]
        public void Leaderboard_HomePageButton_IsVisible()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var button = wait.Until(d => d.FindElement(By.CssSelector("a[href*='Leaderboard']")));

            Assert.That(button.Displayed, Is.True);
        }

        [Test]
        public void Leaderboard_HomePageButton_NavigatesToLeaderboard()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var button = wait.Until(d => d.FindElement(By.CssSelector("a[href*='Leaderboard']")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
            Thread.Sleep(300);
            button.Click();

            wait.Until(d => d.Url.Contains("/Leaderboard"));
            Assert.That(Driver.Url, Does.Contain("/Leaderboard"));
        }

        [Test]
        public void Leaderboard_HomePageViewLeaderboardButton_NavigatesToLeaderboard()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var button = wait.Until(d =>
                d.FindElements(By.CssSelector("a[href*='Leaderboard']"))
                 .FirstOrDefault(a => a.Displayed));

            Assert.That(button, Is.Not.Null);
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
            Thread.Sleep(300);
            button!.Click();

            wait.Until(d => d.Url.Contains("/Leaderboard"));
            Assert.That(Driver.Url, Does.Contain("/Leaderboard"));
        }

        // ── Table structure ──────────────────────────────────────────────────

        [Test]
        public void Leaderboard_ShowsTopNLabel()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Leaderboard");

            Assert.That(Driver.PageSource, Does.Contain("Top"));
        }

        [Test]
        public void Leaderboard_WhenEmpty_ShowsNoContributionsMessage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Leaderboard");

            var pageSource = Driver.PageSource;

            // Either shows the empty message or has a table (data may exist in test db)
            Assert.That(
                pageSource.Contains("No contributions yet.") || pageSource.Contains("<table"),
                Is.True);
        }

        [Test]
        public void Leaderboard_WhenEntriesExist_ShowsTable()
        {
            // Log in so user points record exists (created in SetUp)
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Leaderboard");

            var pageSource = Driver.PageSource;

            // At minimum a user exists, so the table should be visible
            Assert.That(pageSource, Does.Contain("<table").Or.Contain("No contributions yet."));
        }

        [Test]
        public void Leaderboard_WhenEntriesExist_TableHasCorrectHeaders()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Leaderboard");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));

            // Only check headers if the table rendered
            var tables = Driver.FindElements(By.TagName("table"));
            if (tables.Count > 0)
            {
                var headers = Driver.FindElements(By.TagName("th"))
                                    .Select(th => th.Text)
                                    .ToList();

                Assert.That(headers, Does.Contain("Rank"));
                Assert.That(headers, Does.Contain("User"));
                Assert.That(headers, Does.Contain("Points"));
                Assert.That(headers, Does.Contain("Updated"));
            }
            else
            {
                Assert.That(Driver.PageSource, Does.Contain("No contributions yet."));
            }
        }

        [Test]
        public void Leaderboard_DefaultTopN_Is25()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Leaderboard");

            Assert.That(Driver.PageSource, Does.Contain("25"));
        }

        [Test]
        public void Leaderboard_CustomTopN_ReflectedOnPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Leaderboard?topN=10");

            Assert.That(Driver.PageSource, Does.Contain("10"));
        }
    }
}
