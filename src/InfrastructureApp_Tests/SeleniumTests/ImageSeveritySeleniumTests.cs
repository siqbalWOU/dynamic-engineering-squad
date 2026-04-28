using System;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace InfrastructureApp_Tests.SeleniumTests
{
    [TestFixture]
    [Category("ImageSeverity")]
    [NonParallelizable]
    public sealed class ImageSeveritySeleniumTests : SeleniumTestBase
    {
        [Test]
        public void ReportIssue_Create_Page_Shows_Image_Severity_Test_Form_Elements()
        {
            Login();

            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Create");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));

            var reportForm = wait.Until(
                ExpectedConditions.ElementExists(By.CssSelector("form[asp-action='Create'], form[action*='/ReportIssue/Create']")));

            var description = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Description")));
            var photo = wait.Until(ExpectedConditions.ElementExists(By.Id("Photo")));
            var latitude = wait.Until(ExpectedConditions.ElementExists(By.Id("Latitude")));
            var longitude = wait.Until(ExpectedConditions.ElementExists(By.Id("Longitude")));

            var submitButton = reportForm.FindElement(
                By.CssSelector("button[type='submit'][aria-label='Submit the issue report']"));

            Assert.That(Driver.Url, Does.Contain("/ReportIssue/Create"));
            Assert.That(description.Displayed, Is.True);
            Assert.That(photo.Displayed, Is.True);
            Assert.That(latitude.GetAttribute("type"), Is.EqualTo("hidden"));
            Assert.That(longitude.GetAttribute("type"), Is.EqualTo("hidden"));
            Assert.That(submitButton.Text.Trim(), Is.EqualTo("Submit"));

            Assert.That(description.GetAttribute("placeholder"), Does.Contain("Describe the infrastructure issue"));
            Assert.That(Driver.PageSource, Does.Contain("Issue Submission Form"));
        }
    }
}