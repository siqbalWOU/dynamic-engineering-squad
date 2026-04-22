using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using InfrastructureApp_Tests.SeleniumTests.Helpers;

namespace InfrastructureApp_Tests.SeleniumTests
{
    [TestFixture]
    [Category("Selenium")]
    public class HomeRecentReportsSeleniumTests : SeleniumTestBase
    {
        [Test]
        public void HomePage_RecentActivitySection_IsVisible()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var heading = Driver.FindElement(By.Id("recentReportsHeading"));

            Assert.That(heading.Displayed, Is.True);
            Assert.That(heading.Text, Does.Contain("Recent Activity"));
        }

        [Test]
        public void HomePage_ViewAllLatestReportsButton_IsVisible()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var button = Driver.FindElement(By.Id("viewAllLatestReportsButton"));

            Assert.That(button.Displayed, Is.True);
            Assert.That(button.Text, Does.Contain("View All Latest Reports"));
        }

        [Test]
        public void HomePage_ViewAllLatestReportsButton_NavigatesToLatestReports()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var button = Driver.FindElement(By.Id("viewAllLatestReportsButton"));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
            Thread.Sleep(500);
            button.Click();

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.Url.Contains("/Reports/Latest"));

            Assert.That(Driver.Url, Does.Contain("/Reports/Latest"));
        }

        [Test]
        public void HomePage_ShowsRecentReportsList_Or_EmptyMessage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var recentReports = Driver.FindElements(By.CssSelector("#recentReportsList .list-group-item"));
            var emptyMessage = Driver.FindElements(By.Id("noRecentReportsMessage"));

            Assert.That(recentReports.Count > 0 || emptyMessage.Count > 0, Is.True);
        }

        [Test]
        public void HomePage_ShowsAtMostThreeRecentReports()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var recentReports = Driver.FindElements(By.CssSelector("#recentReportsList .list-group-item"));

            Assert.That(recentReports.Count, Is.LessThanOrEqualTo(3));
        }
    }
}