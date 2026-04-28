using System;
using System.IO;
using InfrastructureApp.Services.ImageSeverity;
using InfrastructureApp_Tests.TestDoubles;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using SeleniumExtras.WaitHelpers;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public sealed class ImageSeveritySteps
    {
        private IWebDriver? _driver;
        private string? _testImagePath;

        private const string BaseUrl = "http://127.0.0.1:5044";

        [BeforeScenario("ImageSeverity")]
        public void BeforeScenario()
        {
            SeverityTestBehavior.Reset();

            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1280,900");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            _testImagePath = CreateTinyPngFile();
        }

        [AfterScenario("ImageSeverity")]
        public void AfterScenario()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch
            {
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_testImagePath) && File.Exists(_testImagePath))
                {
                    File.Delete(_testImagePath);
                }
            }
            catch
            {
            }

            SeverityTestBehavior.Reset();
        }

        [Given("image moderation passes")]
        public void GivenImageModerationPasses()
        {
            SeverityTestBehavior.ModerationResult = ImageModerationResult.Passed();
        }

        [Given("image moderation rejects the uploaded image")]
        public void GivenImageModerationRejectsTheUploadedImage()
        {
            SeverityTestBehavior.ModerationResult =
                ImageModerationResult.Rejected("Image was flagged by moderation.");
        }

        [Given("image severity estimation succeeds with status {string} and reason {string}")]
        public void GivenImageSeverityEstimationSucceedsWithStatusAndReason(string status, string reason)
        {
            SeverityTestBehavior.SeverityResult =
                SeverityEstimationResult.Success(status, reason);
        }

        [Given("image severity estimation fails")]
        public void GivenImageSeverityEstimationFails()
        {
            SeverityTestBehavior.SeverityResult =
                SeverityEstimationResult.Failed("Estimator unavailable.");
        }

        [Given("I am on the Report Issue page for image severity testing")]
        public void GivenIAmOnTheReportIssuePage()
        {
            _driver!.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Create");

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.ElementExists(By.Id("Description")));
        }

        [When("I submit a report with description {string} and a valid test image")]
        public void WhenISubmitAReportWithDescriptionAndAValidTestImage(string description)
        {
            var descriptionBox = _driver!.FindElement(By.Id("Description"));
            descriptionBox.Clear();
            descriptionBox.SendKeys(description);

            SetHiddenInputValue("Latitude", "44.9429");
            SetHiddenInputValue("Longitude", "-123.0351");

            var photoInput = _driver.FindElement(By.Id("Photo"));
            photoInput.SendKeys(_testImagePath!);

            var submitButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});", submitButton);

            try
            {
                submitButton.Click();
            }
            catch (ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", submitButton);
            }
        }

        [Then("I should be redirected to the report details page")]
        public void ThenIShouldBeRedirectedToTheReportDetailsPage()
        {
            var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Url.Contains("/ReportIssue/Details"));

            Assert.That(_driver.Url, Does.Contain("/ReportIssue/Details"));
        }

        [Then("I should see severity status {string}")]
        public void ThenIShouldSeeSeverityStatus(string expectedStatus)
        {
            var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(10));
            var severityStatus = wait.Until(
                ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='severity-status']")));

            Assert.That(severityStatus.Text.Trim(), Is.EqualTo(expectedStatus));
        }

        [Then("I should see severity reason containing {string}")]
        public void ThenIShouldSeeSeverityReasonContaining(string expectedText)
        {
            var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(10));
            var severityReason = wait.Until(
                ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='severity-reason']")));

            Assert.That(severityReason.Text, Does.Contain(expectedText));
        }

        [Then("I should remain on the Report Issue page")]
        public void ThenIShouldRemainOnTheReportIssuePage()
        {
            Assert.That(_driver!.Url, Does.Contain("/ReportIssue/Create"));
        }

        [Then("I should see an image moderation error message")]
        public void ThenIShouldSeeAnImageModerationErrorMessage()
        {
            var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(10));
            var error = wait.Until(
                ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='submit-error']")));

            Assert.That(error.Text,
                Does.Contain("cannot")
                    .Or.Contain("flagged")
                    .Or.Contain("inappropriate"));
        }

        private void SetHiddenInputValue(string elementId, string value)
        {
            var element = _driver!.FindElement(By.Id(elementId));

            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].value = arguments[1];", element, value);
        }

        private static string CreateTinyPngFile()
        {
            byte[] pngBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9WlH0n0AAAAASUVORK5CYII=");

            var path = Path.Combine(Path.GetTempPath(), $"severity-test-{Guid.NewGuid():N}.png");
            File.WriteAllBytes(path, pngBytes);
            return path;
        }
    }
}