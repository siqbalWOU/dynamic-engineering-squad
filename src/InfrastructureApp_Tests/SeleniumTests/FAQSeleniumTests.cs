using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using InfrastructureApp_Tests.SeleniumTests.Helpers;

namespace InfrastructureApp_Tests.SeleniumTests
{
    [TestFixture]
    [Category("Selenium")]
    public class FAQSeleniumTests : SeleniumTestBase
    {
        // ── Navigation ───────────────────────────────────────────────────────────

        [Test]
        public void FAQ_PageLoads_WithCorrectHeading()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.TagName("h1")));

            Assert.That(Driver.PageSource, Does.Contain("Frequently Asked Questions"));
        }

        [Test]
        public void FAQ_FooterLink_IsVisible()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var footerLink = wait.Until(d =>
                d.FindElements(By.CssSelector("footer a"))
                 .FirstOrDefault(a => a.Text.Contains("FAQ")));

            Assert.That(footerLink, Is.Not.Null);
            Assert.That(footerLink!.Displayed, Is.True);
        }

        [Test]
        public void FAQ_FooterLink_NavigatesToFAQPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var footerLink = wait.Until(d =>
                d.FindElements(By.CssSelector("footer a"))
                 .FirstOrDefault(a => a.Text.Contains("FAQ")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", footerLink);
            Thread.Sleep(300);
            footerLink!.Click();

            wait.Until(d => d.Url.Contains("/Home/FAQ"));
            Assert.That(Driver.Url, Does.Contain("/Home/FAQ"));
        }

        // ── Account Creation Section ─────────────────────────────────────────────

        [Test]
        public void FAQ_AccountCreationSection_IsPresent()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            Assert.That(Driver.PageSource, Does.Contain("Creating Your Account"));
        }

        [Test]
        public void FAQ_WhyCreateAccount_AccordionOpens_AndShowsContent()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var button = wait.Until(d => d.FindElement(By.CssSelector("[data-bs-target='#faq-why-account']")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
            Thread.Sleep(300);
            button.Click();

            wait.Until(d => d.FindElement(By.Id("faq-why-account")).GetAttribute("class").Contains("show"));

            var body = Driver.FindElement(By.Id("faq-why-account"));
            Assert.That(body.Text, Does.Contain("Submit infrastructure issue reports"));
        }

        [Test]
        public void FAQ_HowToRegister_AccordionOpens_AndShowsSteps()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var button = wait.Until(d => d.FindElement(By.CssSelector("[data-bs-target='#faq-how-register']")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
            Thread.Sleep(300);
            button.Click();

            wait.Until(d => d.FindElement(By.Id("faq-how-register")).GetAttribute("class").Contains("show"));

            var body = Driver.FindElement(By.Id("faq-how-register"));
            Assert.That(body.Text, Does.Contain("Register"));
        }

        [Test]
        public void FAQ_PasswordRequirements_AccordionOpens_AndShowsMinLength()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var button = wait.Until(d => d.FindElement(By.CssSelector("[data-bs-target='#faq-password-rules']")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
            Thread.Sleep(300);
            button.Click();

            wait.Until(d => d.FindElement(By.Id("faq-password-rules")).GetAttribute("class").Contains("show"));

            var body = Driver.FindElement(By.Id("faq-password-rules"));
            Assert.That(body.Text, Does.Contain("6 characters"));
            Assert.That(body.Text, Does.Contain("40 characters"));
        }

        [Test]
        public void FAQ_PasswordConfirm_AccordionOpens_AndExplainsMatching()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var button = wait.Until(d => d.FindElement(By.CssSelector("[data-bs-target='#faq-password-confirm']")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
            Thread.Sleep(300);
            button.Click();

            wait.Until(d => d.FindElement(By.Id("faq-password-confirm")).GetAttribute("class").Contains("show"));

            var body = Driver.FindElement(By.Id("faq-password-confirm"));
            Assert.That(body.Text, Does.Contain("identical"));
        }

        [Test]
        public void FAQ_ValidationFailure_AccordionOpens_AndStatesAccountNotCreated()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var button = wait.Until(d => d.FindElement(By.CssSelector("[data-bs-target='#faq-validation-fail']")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
            Thread.Sleep(300);
            button.Click();

            wait.Until(d => d.FindElement(By.Id("faq-validation-fail")).GetAttribute("class").Contains("show"));

            var body = Driver.FindElement(By.Id("faq-validation-fail"));
            Assert.That(body.Text, Does.Contain("will not be created"));
        }

        [Test]
        public void FAQ_AccountSaved_AccordionOpens_AndMentionsSecureStorage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var button = wait.Until(d => d.FindElement(By.CssSelector("[data-bs-target='#faq-account-saved']")));

            // Julian SCRUM-128 test cleanup:
            // Center the FAQ accordion button and click through JS to avoid flaky Selenium click interception.
            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "arguments[0].scrollIntoView({ block: 'center', inline: 'nearest' });",
                button);

            wait.Until(_ => button.Displayed && button.Enabled);
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", button);

            wait.Until(d => d.FindElement(By.Id("faq-account-saved")).GetAttribute("class").Contains("show"));

            var body = Driver.FindElement(By.Id("faq-account-saved"));
            Assert.That(body.Text, Does.Contain("stored"));
        }

        // ── Team Section ────────────────────────────────────────────────────────

        [Test]
        public void FAQ_TeamSection_ShowsAllFourMembers()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            Assert.That(Driver.PageSource, Does.Contain("Julian"));
            Assert.That(Driver.PageSource, Does.Contain("Sunair"));
            Assert.That(Driver.PageSource, Does.Contain("Phillip"));
            Assert.That(Driver.PageSource, Does.Contain("Erin"));
        }

        [Test]
        public void FAQ_TeamSection_MentionsDynamicEngineeringSquad()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Home/FAQ");

            Assert.That(Driver.PageSource, Does.Contain("Dynamic Engineering Squad"));
        }
    }
}
