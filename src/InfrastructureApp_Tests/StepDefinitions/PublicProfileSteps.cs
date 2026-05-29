using System.Net;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp_Tests.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class PublicProfileSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private HttpResponseMessage _response = null!;
        private string _html = string.Empty;
        private int _lastReportId;

        public PublicProfileSteps()
        {
            var dbName = "PublicProfileFeature_" + Guid.NewGuid();

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureTestServices(services =>
                {
                    ReplaceDatabase(services, dbName);

                    services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = "TestScheme";
                        options.DefaultAuthenticateScheme = "TestScheme";
                        options.DefaultChallengeScheme = "TestScheme";
                    }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true
            });
        }

        [Given(@"a user ""([^""]*)"" exists with (\d+) approved reports")]
        public async Task GivenAUserExistsWithApprovedReports(string username, int count)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var userId = Guid.NewGuid().ToString();
            db.Users.Add(new Users
            {
                Id = userId,
                UserName = username,
                NormalizedUserName = username.ToUpperInvariant(),
                Email = $"{username}@example.com",
                NormalizedEmail = $"{username}@example.com".ToUpperInvariant(),
                EmailConfirmed = true
            });
            db.UserPoints.Add(new UserPoints { UserId = userId, CurrentPoints = 0, LifetimePoints = 0 });

            for (var i = 0; i < count; i++)
            {
                db.ReportIssue.Add(new ReportIssue
                {
                    UserId = userId,
                    Description = $"Report {i + 1} by {username}",
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow.AddHours(-i)
                });
            }

            await db.SaveChangesAsync();
        }

        [Given(@"a user ""([^""]*)"" exists with a report titled ""([^""]*)"" on ""([^""]*)""")]
        public async Task GivenAUserExistsWithAReportTitledOnDate(string username, string title, string date)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var userId = Guid.NewGuid().ToString();
            db.Users.Add(new Users
            {
                Id = userId,
                UserName = username,
                NormalizedUserName = username.ToUpperInvariant(),
                Email = $"{username}@example.com",
                NormalizedEmail = $"{username}@example.com".ToUpperInvariant(),
                EmailConfirmed = true
            });
            db.UserPoints.Add(new UserPoints { UserId = userId, CurrentPoints = 0, LifetimePoints = 0 });

            var report = new ReportIssue
            {
                UserId = userId,
                IssueName = title,
                Description = title,
                Status = "Approved",
                CreatedAt = DateTime.Parse(date)
            };
            db.ReportIssue.Add(report);
            await db.SaveChangesAsync();
            _lastReportId = report.Id;
        }

        [Given(@"a user ""([^""]*)"" exists with a report titled ""([^""]*)""")]
        public async Task GivenAUserExistsWithAReportTitled(string username, string title)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var userId = Guid.NewGuid().ToString();
            db.Users.Add(new Users
            {
                Id = userId,
                UserName = username,
                NormalizedUserName = username.ToUpperInvariant(),
                Email = $"{username}@example.com",
                NormalizedEmail = $"{username}@example.com".ToUpperInvariant(),
                EmailConfirmed = true
            });
            db.UserPoints.Add(new UserPoints { UserId = userId, CurrentPoints = 0, LifetimePoints = 0 });

            var report = new ReportIssue
            {
                UserId = userId,
                IssueName = title,
                Description = title,
                Status = "Approved",
                CreatedAt = DateTime.UtcNow
            };
            db.ReportIssue.Add(report);
            await db.SaveChangesAsync();
            _lastReportId = report.Id;
        }

        [Given(@"a user ""([^""]*)"" exists with email ""([^""]*)""")]
        public async Task GivenAUserExistsWithEmail(string username, string email)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var userId = Guid.NewGuid().ToString();
            db.Users.Add(new Users
            {
                Id = userId,
                UserName = username,
                NormalizedUserName = username.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true
            });
            db.UserPoints.Add(new UserPoints { UserId = userId, CurrentPoints = 0, LifetimePoints = 0 });
            await db.SaveChangesAsync();
        }

        [Given(@"a user ""([^""]*)"" exists with no reports")]
        public async Task GivenAUserExistsWithNoReports(string username)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var userId = Guid.NewGuid().ToString();
            db.Users.Add(new Users
            {
                Id = userId,
                UserName = username,
                NormalizedUserName = username.ToUpperInvariant(),
                Email = $"{username}@example.com",
                NormalizedEmail = $"{username}@example.com".ToUpperInvariant(),
                EmailConfirmed = true
            });
            db.UserPoints.Add(new UserPoints { UserId = userId, CurrentPoints = 0, LifetimePoints = 0 });
            await db.SaveChangesAsync();
        }

        [Given(@"I am on the Leaderboard page")]
        public async Task GivenIAmOnTheLeaderboardPage()
        {
            _response = await _client.GetAsync("/Leaderboard");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [When(@"I click on the username ""(.*)""")]
        public async Task WhenIClickOnTheUsername(string username)
        {
            _response = await _client.GetAsync($"/Dashboard?username={username}");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Then(@"I should be redirected to ""(.*)""'s public profile page")]
        public void ThenIShouldBeRedirectedToPublicProfilePage(string username)
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Contain(username));
        }

        [Then(@"I should see ""(.*)""'s username")]
        public void ThenIShouldSeeUsernameOnProfile(string username)
        {
            Assert.That(_html, Does.Contain(username));
        }

        [Then(@"I should see (\d+) reports listed in the contribution feed")]
        public void ThenIShouldSeeReportsListedInTheContributionFeed(int count)
        {
            var occurrences = CountOccurrences(_html, "data-testid=\"profile-report-item\"");
            Assert.That(occurrences, Is.EqualTo(count));
        }

        [When(@"I navigate to ""(.*)""'s public profile page")]
        public async Task WhenINavigateToPublicProfilePage(string username)
        {
            _response = await _client.GetAsync($"/Dashboard?username={username}");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Then(@"I should see a report with title ""(.*)"" and date ""(.*)""")]
        public void ThenIShouldSeeReportWithTitleAndDate(string title, string date)
        {
            Assert.That(_html, Does.Contain(title));
            var expectedDate = DateTime.Parse(date).ToString("MMM d, yyyy");
            Assert.That(_html, Does.Contain(expectedDate));
        }

        [When(@"I click on the report title ""(.*)""")]
        public async Task WhenIClickOnTheReportTitle(string title)
        {
            _response = await _client.GetAsync($"/ReportIssue/Details/{_lastReportId}");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Then(@"I should be redirected to the full details page for that report")]
        public void ThenIShouldBeRedirectedToFullDetailsPage()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Contain("Report Submitted"));
        }

        [Scope(Feature = "Public User Profile")]
        [Then(@"I should not see ""(.*)""")]
        public void ThenIShouldNotSeeText(string text)
        {
            Assert.That(_html, Does.Not.Contain(text));
        }

        [Then(@"I should not see account settings links")]
        public void ThenIShouldNotSeeAccountSettingsLinks()
        {
            Assert.That(_html, Does.Not.Contain("Delete Account"));
        }

        [Scope(Feature = "Public User Profile")]
        [Then(@"I should see ""(.*)""")]
        public void ThenIShouldSeeText(string text)
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Contain(text));
        }

        [Then(@"I should see the first 10 reports")]
        public void ThenIShouldSeeTheFirst10Reports()
        {
            var occurrences = CountOccurrences(_html, "data-testid=\"profile-report-item\"");
            Assert.That(occurrences, Is.EqualTo(10));
        }

        [Then(@"I should see pagination controls")]
        public void ThenIShouldSeePaginationControls()
        {
            Assert.That(_html, Does.Contain("data-testid=\"profile-pagination\""));
        }

        private static int CountOccurrences(string source, string search)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index++;
            }
            return count;
        }

        private static void ReplaceDatabase(IServiceCollection services, string dbName)
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(ApplicationDbContext) ||
                d.ServiceType.Name.Contains("DbContextOptions")).ToList();

            foreach (var descriptor in descriptors)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
