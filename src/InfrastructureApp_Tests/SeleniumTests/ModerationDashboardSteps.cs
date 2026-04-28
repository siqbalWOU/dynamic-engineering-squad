using System;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class ModerationDashboardSteps : SeleniumTestBase
    {
        private readonly ScenarioContext _scenarioContext;

        public ModerationDashboardSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [Given(@"I am not logged in")]
        [Scope(Feature = "Moderation Dashboard")]
        public void GivenIAmNotLoggedIn()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Logout");
        }

        [When(@"I attempt to access the moderation dashboard URL")]
        [Scope(Feature = "Moderation Dashboard")]
        public void WhenIAttemptToAccessTheModerationDashboardURL()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Moderation");
        }

        [Then(@"I should be redirected to the home page or login page")]
        [Scope(Feature = "Moderation Dashboard")]
        public void ThenIShouldBeRedirectedToTheHomePageOrLoginPage()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Url == $"{BaseUrl}/" 
                         || d.Url == $"{BaseUrl}/Home/Index" 
                         || d.Url.Contains("/Login")
                         || d.Url.Contains("/AccessDenied"));
        }

        [Given(@"I am logged in as ""(.*)""")]
        [Scope(Feature = "Moderation Dashboard")]
        public async Task GivenIAmLoggedInAs(string username)
        {
            await CreateTestUser(username, "Password123!");
            Login(username, "Password123!");
            _scenarioContext["CurrentUsername"] = username;
        }

        [Given(@"I do not have ""Moderator"" or ""Admin"" roles")]
        [Scope(Feature = "Moderation Dashboard")]
        public void GivenIDoNotHaveOrRoles()
        {
            // Default user has no roles
        }

        [Given(@"I have the ""(.*)"" role")]
        [Scope(Feature = "Moderation Dashboard")]
        public async Task GivenIHaveTheRole(string roleName)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var username = _scenarioContext["CurrentUsername"].ToString();
            var user = await userManager.FindByNameAsync(username!);
            
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            if (!await userManager.IsInRoleAsync(user!, roleName))
            {
                await userManager.AddToRoleAsync(user!, roleName);
            }
            
            // Refresh login
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Logout");
            Login(username!, "Password123!");
        }

        [Then(@"I should see a ""(.*)"" link in the navbar")]
        [Scope(Feature = "Moderation Dashboard")]
        public void ThenIShouldSeeALinkInTheNavbar(string linkText)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElements(By.LinkText(linkText)).Count > 0);
            var links = Driver.FindElements(By.LinkText(linkText));
            Assert.That(links.Count, Is.GreaterThan(0), $"Link with text '{linkText}' not found in navbar.");
        }

        [When(@"I click the ""(.*)"" link")]
        [Scope(Feature = "Moderation Dashboard")]
        public void WhenIClickTheLink(string linkText)
        {
            var link = Driver.FindElement(By.LinkText(linkText));
            ScrollAndClick(link);
        }

        [Then(@"I should be on the Moderation Dashboard page")]
        [Scope(Feature = "Moderation Dashboard")]
        public void ThenIShouldBeOnTheModerationDashboardPage()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Url.Contains("/Moderation"));
            Assert.That(Driver.Url, Does.Contain("/Moderation"));
        }

        [Then(@"I should see ""(.*)"" heading")]
        [Scope(Feature = "Moderation Dashboard")]
        public void ThenIShouldSeeHeading(string headingText)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var heading = wait.Until(d => d.FindElement(By.TagName("h1")));
            Assert.That(heading.Text, Does.Contain(headingText));
        }

        [Given(@"a report exists with description ""(.*)""")]
        [Scope(Feature = "Moderation Dashboard")]
        public async Task GivenAReportExistsWithDescription(string description)
        {
            await CreateTestUser("selenium-mod-test-user", "Password123!");
            
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await userManager.FindByNameAsync("selenium-mod-test-user");

            var report = new ReportIssue
            {
                Description = description,
                Status = "Approved",
                UserId = user!.Id,
                CreatedAt = DateTime.UtcNow
            };

            db.ReportIssue.Add(report);
            await db.SaveChangesAsync();
            _scenarioContext["LastReportId"] = report.Id;
        }

        [Given(@"it has been flagged with category ""([^""]*)"" by ""([^""]*)""")]
        [Scope(Feature = "Moderation Dashboard")]
        public async Task GivenItHasBeenFlaggedWithCategoryBy(string category, string username)
        {
            await CreateTestUser(username, "Password123!");
            var reportId = (int)_scenarioContext["LastReportId"];
            
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await userManager.FindByNameAsync(username);
            
            db.ReportFlags.Add(new ReportFlag
            {
                ReportIssueId = reportId,
                UserId = user!.Id,
                Category = category,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        [Given(@"it has been flagged with category ""([^""]*)""$")]
        [Scope(Feature = "Moderation Dashboard")]
        public async Task GivenItHasBeenFlaggedWithCategory(string category)
        {
            await CreateTestUser("flag-user", "Password123!");
            var reportId = (int)_scenarioContext["LastReportId"];
            
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await userManager.FindByNameAsync("flag-user");
            
            db.ReportFlags.Add(new ReportFlag
            {
                ReportIssueId = reportId,
                UserId = user!.Id,
                Category = category,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        [Given(@"I am logged in as a moderator")]
        [Scope(Feature = "Moderation Dashboard")]
        public async Task GivenIAmLoggedInAsAModerator()
        {
            await GivenIAmLoggedInAs("moduser");
            await GivenIHaveTheRole("Moderator");
        }

        [When(@"I navigate to the moderation dashboard")]
        [Given(@"I am on the moderation dashboard")]
        [Scope(Feature = "Moderation Dashboard")]
        public void WhenINavigateToTheModerationDashboard()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Moderation");
        }

        [Then(@"I should see the report with description ""(.*)""")]
        [Scope(Feature = "Moderation Dashboard")]
        public void ThenIShouldSeeTheReportWithDescription(string description)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains(description));
            var bodyText = Driver.FindElement(By.TagName("body")).Text;
            Assert.That(bodyText, Does.Contain(description));
        }

        [Then(@"it should show {int} flag")]
        [Scope(Feature = "Moderation Dashboard")]
        public void ThenItShouldShowFlag(int count)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains(count.ToString()));
            var bodyText = Driver.FindElement(By.TagName("body")).Text;
            Assert.That(bodyText, Does.Contain(count.ToString()));
        }

        [Then(@"the flag reason should include ""(.*)""")]
        [Scope(Feature = "Moderation Dashboard")]
        public void ThenTheFlagReasonShouldInclude(string category)
        {
            Assert.That(Driver.PageSource, Does.Contain(category));
        }

        [When(@"I click ""Dismiss Report"" for the report ""(.*)""")]
        [Scope(Feature = "Moderation Dashboard")]
        public void WhenIClickDismissReportFortheReport(string description)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var dismissBtn = wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{description}')]//button[contains(., 'Dismiss')]")));
            ScrollAndClick(dismissBtn);
        }

        [Then(@"the report ""(.*)"" should no longer be in the moderation queue")]
        [Scope(Feature = "Moderation Dashboard")]
        public void ThenTheReportShouldNoLongerBeInTheModerationQueue(string description)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => !d.PageSource.Contains(description));
            Assert.That(Driver.PageSource, Does.Not.Contain(description));
        }

        [Then(@"the report ""(.*)"" should still exist in the system")]
        [Scope(Feature = "Moderation Dashboard")]
        public async Task ThenTheReportShouldStillExistInTheSystem(string description)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var exists = await db.ReportIssue.AnyAsync(r => r.Description == description);
            Assert.That(exists, Is.True);
        }

        [When(@"I click ""Remove Post"" for the report ""(.*)""")]
        [Scope(Feature = "Moderation Dashboard")]
        public void WhenIClickRemovePostForTheReport(string description)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            var removeBtn = wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{description}')]//button[contains(., 'Remove')]")));
            ScrollAndClick(removeBtn);
        }

        [Then(@"I should see a confirmation prompt")]
        [Scope(Feature = "Moderation Dashboard")]
        public void ThenIShouldSeeAConfirmationPrompt()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var modal = wait.Until(d => {
                var m = d.FindElement(By.Id("confirmRemoveModal"));
                return m.Displayed ? m : null;
            });
            Assert.That(modal, Is.Not.Null);
        }

        [When(@"I confirm the removal")]
        [Scope(Feature = "Moderation Dashboard")]
        public void WhenIConfirmTheRemoval()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var confirmBtn = wait.Until(d => d.FindElement(By.Id("confirmRemoveBtn")));
            ScrollAndClick(confirmBtn);
        }

        [Then(@"the report ""(.*)"" should no longer exist in the system")]
        [Scope(Feature = "Moderation Dashboard")]
        public async Task ThenTheReportShouldNoLongerExistInTheSystem(string description)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(async d => {
                using var scope = ServerHost!.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return !await db.ReportIssue.AnyAsync(r => r.Description == description);
            });
        }

        [Then(@"a moderation action should be logged for ""(.*)"" ""(.*)""")]
        [Scope(Feature = "Moderation Dashboard")]
        public async Task ThenAModerationActionShouldBeLoggedFor(string action, string description)
        {
             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
             wait.Until(async d => {
                 using var scope = ServerHost!.Services.CreateScope();
                 var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                 return await db.ModerationActionLogs.AnyAsync(l => l.Action == action && l.TargetContentSnapshot.Contains(description));
             });
        }

        private async Task<int> GetLastReportId()
        {
            using var scope = ServerHost!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return (await db.ReportIssue.OrderByDescending(r => r.Id).FirstAsync()).Id;
        }
    }
}
