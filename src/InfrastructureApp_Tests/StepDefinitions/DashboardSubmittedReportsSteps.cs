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
    // SCRUM-137: Step definitions for viewing submitted reports on the private Dashboard.
    [Binding]
    public class DashboardSubmittedReportsSteps : IDisposable
    {
        private const string CurrentUserId = "1";
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private HttpResponseMessage _response = null!;
        private string _html = string.Empty;

        public DashboardSubmittedReportsSteps()
        {
            var dbName = "DashboardSubmittedReportsFeature_" + Guid.NewGuid();

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

        // TEST 1: Set up the logged-in Dashboard user used by SCRUM-137 scenarios.
        [Given("I am logged in as a Dashboard user")]
        public async Task GivenIAmLoggedInAsADashboardUser()
        {
            // Arrange: seed the authenticated test user id used by TestAuthHandler.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!await db.Users.AnyAsync(u => u.Id == CurrentUserId))
            {
                db.Users.Add(new Users
                {
                    Id = CurrentUserId,
                    UserName = "DashboardUser",
                    Email = "dashboard@test.com",
                    EmailConfirmed = true
                });
            }

            await db.SaveChangesAsync();
        }

        // TEST 2: Seed a submitted report owned by the logged-in Dashboard user.
        [Given("I have submitted a report with description {string}")]
        public async Task GivenIHaveSubmittedAReportWithDescription(string description)
        {
            // Arrange: add a report tied to the authenticated user's id.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.ReportIssue.Add(new ReportIssue
            {
                UserId = CurrentUserId,
                Description = description,
                Status = "Approved",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        // TEST 3: Seed a submitted report owned by a different user.
        [Given("another user submitted a report with description {string}")]
        public async Task GivenAnotherUserSubmittedAReportWithDescription(string description)
        {
            // Arrange: add another user and a report that should not appear on the Dashboard.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var otherUserId = Guid.NewGuid().ToString();

            db.Users.Add(new Users
            {
                Id = otherUserId,
                UserName = "OtherDashboardUser",
                Email = "otherdashboard@test.com",
                EmailConfirmed = true
            });

            db.ReportIssue.Add(new ReportIssue
            {
                UserId = otherUserId,
                Description = description,
                Status = "Approved",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        [When("I visit my Dashboard")]
        public async Task WhenIVisitMyDashboard()
        {
            // Act: request the authenticated user's private Dashboard.
            _response = await _client.GetAsync("/Dashboard");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Then("I should see {string}")]
        public void ThenIShouldSee(string expectedText)
        {
            // Assert: the Dashboard response contains the expected text.
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Contain(expectedText));
        }

        [Then("I should see my submitted report with description {string}")]
        public void ThenIShouldSeeMySubmittedReportWithDescription(string description)
        {
            // Assert: the current user's submitted report appears in the Dashboard HTML.
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Contain(description));
        }

        [Then("I should not see a submitted report with description {string}")]
        public void ThenIShouldNotSeeASubmittedReportWithDescription(string description)
        {
            // Assert: another user's report is excluded from the private Dashboard HTML.
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Not.Contain(description));
        }

        private static void ReplaceDatabase(IServiceCollection services, string dbName)
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(ApplicationDbContext) ||
                d.ServiceType.Name.Contains("DbContextOptions")).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

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
