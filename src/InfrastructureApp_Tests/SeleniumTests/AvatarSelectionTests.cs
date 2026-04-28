using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using InfrastructureApp_Tests.SeleniumTests.Helpers;

namespace InfrastructureApp_Tests.SeleniumTests
{
    [TestFixture]
    public class AvatarSelectionTests : SeleniumTestBase
    {
        [Test]
        public void ChooseAvatar_PageLoads_AfterLogin()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ChooseAvatar");

            //Verifies that page contains "Choose your avatar" and that at least one avatar tile exists
            //Ensures page renders correctly
            Assert.That(Driver.PageSource, Does.Contain("Choose Your Avatar"));
            Assert.That(Driver.FindElements(By.CssSelector(".avatar-tile")).Count, Is.GreaterThan(0));
        }

        [Test]
        public void ChooseAvatar_SelectPresetTile_HighlightsTile()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ChooseAvatar");


            //Clicks first avatar tile
            var firstTile = Driver.FindElement(By.CssSelector(".avatar-tile"));
            firstTile.Click();

            // Tile should have 'selected' class after clicking
            Assert.That(firstTile.GetAttribute("class"), Does.Contain("selected"));
        }

        [Test]
        public void ChooseAvatar_SelectPresetTile_SetsHiddenInput()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ChooseAvatar");

            var firstTile = Driver.FindElement(By.CssSelector(".avatar-tile"));
            var expectedKey = firstTile.GetAttribute("data-avatar-key");
            firstTile.Click();

            var hiddenInput = Driver.FindElement(By.CssSelector("input[name='SelectedAvatarKey']"));
            Assert.That(hiddenInput.GetAttribute("value"), Is.EqualTo(expectedKey));
        }

        [Test]
        public void ChooseAvatar_SelectAndSavePreset_RedirectsToHome()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ChooseAvatar");

            // Click first tile then save
            Driver.FindElement(By.CssSelector(".avatar-tile")).Click();
            var saveBtn = Driver.FindElement(By.Id("saveBtn"));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", saveBtn);
            Thread.Sleep(500);
            saveBtn.Click();

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.Url.Contains("/Home") || d.Url == $"{BaseUrl}/");

            Assert.That(Driver.Url, Does.Contain("/Home").Or.EqualTo($"{BaseUrl}/"));
        }

        [Test]
        public void ChooseAvatar_UploadZone_IsVisible()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ChooseAvatar");

            var dropZone = Driver.FindElement(By.Id("dropZone"));
            Assert.That(dropZone.Displayed, Is.True);
        }

        [Test]
        public void ChooseAvatar_UploadValidImage_ShowsPreview()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ChooseAvatar");

            // Create a small temp PNG file to upload
            var tempFile = Path.Combine(Path.GetTempPath(), "test_avatar.png");
            File.WriteAllBytes(tempFile, GenerateMinimalPng());

            // Send file path directly to the hidden file input
            var fileInput = Driver.FindElement(By.Id("UploadedImage"));
            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "arguments[0].classList.remove('d-none');", fileInput);
            fileInput.SendKeys(tempFile);

            // Wait for preview to appear
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElement(By.Id("previewArea"))
                             .GetAttribute("class")
                             .Contains("d-none") == false);

            var previewArea = Driver.FindElement(By.Id("previewArea"));
            Assert.That(previewArea.GetAttribute("class"), Does.Not.Contain("d-none"));

            File.Delete(tempFile);
        }

        [Test]
        public void ChooseAvatar_ClearPhoto_ResetsUploadZone()
        {
            Login();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/ChooseAvatar");

            var tempFile = Path.Combine(Path.GetTempPath(), "test_avatar.png");
            File.WriteAllBytes(tempFile, GenerateMinimalPng());

            var fileInput = Driver.FindElement(By.Id("UploadedImage"));
            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "arguments[0].classList.remove('d-none');", fileInput);
            fileInput.SendKeys(tempFile);

            // Wait for preview then click clear
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElement(By.Id("previewArea"))
                             .GetAttribute("class").Contains("d-none") == false);

            //scroll into view first then click
            var clearBtn = Driver.FindElement(By.Id("clearPhoto"));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", clearBtn);
            Thread.Sleep(500); // small pause after scroll
            clearBtn.Click();

            // Drop zone body should be visible again
            var dropBody = Driver.FindElement(By.Id("dropZoneBody"));
            Assert.That(dropBody.GetAttribute("class"), Does.Not.Contain("d-none"));

            File.Delete(tempFile);
        }

        // Generates a minimal valid 1x1 PNG in memory
        private static byte[] GenerateMinimalPng()
        {
            return Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
        }
    }
}