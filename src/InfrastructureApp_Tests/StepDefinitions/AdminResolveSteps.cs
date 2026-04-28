using System.Net;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class AdminResolveSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;
        private HttpResponseMessage _response = null!;
        private string _html = string.Empty;
        private int _lastReportId;
        private readonly string _dbName;

        public AdminResolveSteps()
        {
            _dbName = "AdminResolveTest_" + Guid.NewGuid();

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
                        services.Remove(d);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(_dbName)
                               .ConfigureWarnings(w => w.Ignore(
                                   Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Given("an approved report exists with description {string}")]
        public async Task GivenAnApprovedReportExistsWithDescription(string description)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var report = new ReportIssue
            {
                Description = description,
                Status = "Approved",
                UserId = "test-user-id",
                CreatedAt = DateTime.UtcNow
            };

            db.ReportIssue.Add(report);
            await db.SaveChangesAsync();
            _lastReportId = report.Id;
        }

        [Given("a resolved report exists with description {string}")]
        public async Task GivenAResolvedReportExistsWithDescription(string description)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var report = new ReportIssue
            {
                Description = description,
                Status = "Resolved",
                UserId = "test-user-id",
                CreatedAt = DateTime.UtcNow
            };

            db.ReportIssue.Add(report);
            await db.SaveChangesAsync();
            _lastReportId = report.Id;
        }

        [When("I navigate to that approved report's details page")]
        public async Task WhenINavigateToThatApprovedReportsDetailsPage()
        {
            _response = await _client.GetAsync($"/ReportIssue/Details/{_lastReportId}");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [When("I request the verify status for that report")]
        public async Task WhenIRequestTheVerifyStatusForThatReport()
        {
            _response = await _client.GetAsync($"/VerifyFix/Status/{_lastReportId}");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [When("I navigate to the Verify Fixes page")]
        public async Task WhenINavigateToTheVerifyFixesPage()
        {
            _response = await _client.GetAsync("/Reports/Verify");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [When("I post to mark that report as resolved without being logged in")]
        public async Task WhenIPostToMarkThatReportAsResolvedWithoutBeingLoggedIn()
        {
            _response = await _client.PostAsync($"/ReportIssue/MarkResolved/{_lastReportId}", null);
        }

        [When("I post to mark that report as verified fixed without being logged in")]
        public async Task WhenIPostToMarkThatReportAsVerifiedFixedWithoutBeingLoggedIn()
        {
            _response = await _client.PostAsync($"/ReportIssue/MarkVerifiedFixed/{_lastReportId}", null);
        }

        [Then("the details page should contain {string}")]
        public void ThenTheDetailsPageShouldContain(string expectedText)
        {
            Assert.That(_html, Does.Contain(expectedText));
        }

        [Then("the verify status response should be 200 OK")]
        public void ThenTheVerifyStatusResponseShouldBe200OK()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Then("the verify status should contain {string}")]
        public void ThenTheVerifyStatusShouldContain(string expectedText)
        {
            Assert.That(_html, Does.Contain(expectedText));
        }

        [Then("the verify fixes page should load successfully")]
        public void ThenTheVerifyFixesPageShouldLoadSuccessfully()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Then("I should be redirected to login")]
        public void ThenIShouldBeRedirectedToLogin()
        {
            Assert.That(
                _response.StatusCode == HttpStatusCode.Redirect ||
                _response.StatusCode == HttpStatusCode.Found ||
                _response.StatusCode == HttpStatusCode.MovedPermanently ||
                (_response.Headers.Location?.ToString().Contains("Login") ?? false),
                Is.True,
                $"Expected redirect to login but got {_response.StatusCode}");
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
