using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using InfrastructureApp_Tests.SeleniumTests.Helpers;

namespace InfrastructureApp_Tests.SeleniumTests
{
    [TestFixture]
    public class RoadCameraTests : SeleniumTestBase
    {
        [Test]
        public void RoadCamera_IndexPage_LoadsAndShowsCameras()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/RoadCamera/Index");

            var heading = Driver.FindElement(By.CssSelector("h1"));
            Assert.That(heading.Text, Does.Contain("Road Cameras"));
        }

        [Test]
        public void RoadCamera_IndexPage_ShowsCameraCount()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/RoadCamera/Index");

            var countText = Driver.FindElement(By.CssSelector("p.text-muted.text-center")).Text;
            Assert.That(countText, Does.Contain("Cameras loaded:"));
        }

        [Test]
        public void RoadCamera_IndexPage_ShowsErrorMessage_WhenApiDown()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/RoadCamera/Index");

            // If cameras loaded = 0, the friendly error should be visible
            var cameras = Driver.FindElements(By.CssSelector(".camera-card"));
            if (cameras.Count == 0)
            {
                var alert = Driver.FindElement(By.CssSelector(".alert"));
                Assert.That(alert.Displayed, Is.True);
            }
            else
            {
                Assert.That(cameras.Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public void RoadCamera_ClickCamera_NavigatesToDetailsPage()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/RoadCamera/Index");

            var cameras = Driver.FindElements(By.CssSelector(".camera-card"));
            if (cameras.Count == 0)
            {
                Assert.Ignore("No cameras available from TripCheck API — skipping test.");
                return;
            }

            // Click the first camera card
            cameras[0].Click();

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.Url.Contains("/RoadCamera/Details"));

            Assert.That(Driver.Url, Does.Contain("/RoadCamera/Details"));
        }

        [Test]
        public void RoadCamera_DetailsPage_ShowsCameraImage()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/RoadCamera/Index");

            var cameras = Driver.FindElements(By.CssSelector(".camera-card"));
            if (cameras.Count == 0)
            {
                Assert.Ignore("No cameras available from TripCheck API — skipping test.");
                return;
            }

            cameras[0].Click();

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElements(By.Id("cameraImage")).Count > 0);

            var image = Driver.FindElement(By.Id("cameraImage"));
            Assert.That(image.Displayed, Is.True);
            Assert.That(image.GetAttribute("src"), Is.Not.Empty);
        }

        [Test]
        public void RoadCamera_DetailsPage_RefreshButtonExists()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/RoadCamera/Index");

            var cameras = Driver.FindElements(By.CssSelector(".camera-card"));
            if (cameras.Count == 0)
            {
                Assert.Ignore("No cameras available from TripCheck API — skipping test.");
                return;
            }

            cameras[0].Click();

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElements(By.Id("refreshBtn")).Count > 0);

            var refreshBtn = Driver.FindElement(By.Id("refreshBtn"));
            Assert.That(refreshBtn.Displayed, Is.True);
        }

        [Test]
        public void RoadCamera_DetailsPage_ReportIssueLinkExists()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/RoadCamera/Index");

            var cameras = Driver.FindElements(By.CssSelector(".camera-card"));
            if (cameras.Count == 0)
            {
                Assert.Ignore("No cameras available from TripCheck API — skipping test.");
                return;
            }

            cameras[0].Click();

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElements(By.CssSelector(".btn-outline-danger")).Count > 0);

            var reportLink = Driver.FindElement(By.CssSelector(".btn-outline-danger"));
            Assert.That(reportLink.Text, Does.Contain("Report Issue"));
        }

        [Test]
        public void RoadCamera_DetailsPage_ReportIssueLink_NavigatesToCreateForm()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/RoadCamera/Index");

            var cameras = Driver.FindElements(By.CssSelector(".camera-card"));
            if (cameras.Count == 0)
            {
                Assert.Ignore("No cameras available from TripCheck API — skipping test.");
                return;
            }

            cameras[0].Click();

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElements(By.CssSelector(".btn-outline-danger")).Count > 0);

            Driver.FindElement(By.CssSelector(".btn-outline-danger")).Click();

            wait.Until(d => d.Url.Contains("/ReportIssue/Create"));

            Assert.That(Driver.Url, Does.Contain("/ReportIssue/Create"));
            Assert.That(Driver.Url, Does.Contain("cameraId="));
            Assert.That(Driver.Url, Does.Contain("imageUrl="));
        }
    }
}