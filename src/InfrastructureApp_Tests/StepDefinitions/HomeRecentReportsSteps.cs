using System.Net;
using Microsoft.AspNetCore.Hosting;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class HomeRecentReportsSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly ScenarioContext _scenarioContext;
        private HttpClient _client = null!;
        private HttpResponseMessage _response = null!;
        private string _html = string.Empty;
        private readonly string _dbName;

        public HomeRecentReportsSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            _dbName = "HomeRecentReportsTest_" + Guid.NewGuid();

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                        d.ServiceType == typeof(ApplicationDbContext) ||
                        d.ServiceType.Name.Contains("DbContextOptions")).ToList();

                    foreach (var d in descriptors)
                    {
                        services.Remove(d);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(_dbName);
                    });
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true
            });
        }

        [Given("I am on the Home page")]
        public async Task GivenIAmOnTheHomePage()
        {
            _response = await _client.GetAsync("/");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Given("recent reports exist in the system")]
        public async Task GivenRecentReportsExistInTheSystem()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.ReportIssue.AddRange(
                new ReportIssue
                {
                    Description = "Broken streetlight on Main St",
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    UserId = "test-user-1"
                },
                new ReportIssue
                {
                    Description = "Large pothole near downtown",
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    UserId = "test-user-2"
                });

            await db.SaveChangesAsync();
        }

        [Given("more than three recent reports exist in the system")]
        public async Task GivenMoreThanThreeRecentReportsExistInTheSystem()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.ReportIssue.AddRange(
                new ReportIssue { Description = "Report 1", Status = "Approved", CreatedAt = DateTime.UtcNow.AddMinutes(-1), UserId = "u1" },
                new ReportIssue { Description = "Report 2", Status = "Approved", CreatedAt = DateTime.UtcNow.AddMinutes(-2), UserId = "u2" },
                new ReportIssue { Description = "Report 3", Status = "Approved", CreatedAt = DateTime.UtcNow.AddMinutes(-3), UserId = "u3" },
                new ReportIssue { Description = "Report 4", Status = "Approved", CreatedAt = DateTime.UtcNow.AddMinutes(-4), UserId = "u4" }
            );

            await db.SaveChangesAsync();
        }

        [Given("no recent reports exist in the system")]
        public async Task GivenNoRecentReportsExistInTheSystem()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.ReportIssue.RemoveRange(db.ReportIssue);
            await db.SaveChangesAsync();
        }

        [When("I visit the Home page")]
        public async Task WhenIVisitTheHomePage()
        {
            _response = await _client.GetAsync("/");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Then("the Recent Activity section should be displayed")]
        public void ThenTheRecentActivitySectionShouldBeDisplayed()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Contain("Recent Activity in Your"));
            Assert.That(_html, Does.Contain("Zone"));
        }

        [Then("recent reports should be displayed on the Home page")]
        public void ThenRecentReportsShouldBeDisplayedOnTheHomePage()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Contain("Broken streetlight on Main St").Or.Contain("Large pothole near downtown"));
        }

        [Then("only three recent reports should be displayed")]
        public void ThenOnlyThreeRecentReportsShouldBeDisplayed()
        {
            var count =
                (_html.Contains("Report 1") ? 1 : 0) +
                (_html.Contains("Report 2") ? 1 : 0) +
                (_html.Contains("Report 3") ? 1 : 0) +
                (_html.Contains("Report 4") ? 1 : 0);

            Assert.That(count, Is.EqualTo(3));
        }

        [Then("a no recent reports message should be displayed")]
        public void ThenANoRecentReportsMessageShouldBeDisplayed()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Contain("No recent reports are available right now."));
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}