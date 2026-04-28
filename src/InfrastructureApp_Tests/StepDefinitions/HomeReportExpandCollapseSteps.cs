using System.Net;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class HomeReportExpandCollapseSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _dbName;
        private HttpClient _client = null!;
        private HttpResponseMessage _response = null!;
        private string _html = string.Empty;

        public HomeReportExpandCollapseSteps()
        {
            _dbName = "HomeReportExpandCollapseTest_" + Guid.NewGuid();

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureServices(services =>
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

        // SCRUM-128:
        // TEST 1 setup: seed Home page reports for expand/collapse markup checks
        [Given("Home page recent reports exist for expand collapse")]
        public async Task GivenHomePageRecentReportsExistForExpandCollapse()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.ReportIssue.AddRange(
                new ReportIssue
                {
                    Description = "Long pothole report for Home expand collapse",
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-3),
                    UserId = "expand-user-1"
                },
                new ReportIssue
                {
                    Description = "Broken sign report for Home expand collapse",
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                    UserId = "expand-user-2"
                });

            await db.SaveChangesAsync();
        }

        // SCRUM-128:
        // TEST 1 action: load the Home page that contains the recent reports list
        [When("I visit the Home page for expand collapse")]
        public async Task WhenIVisitTheHomePageForExpandCollapse()
        {
            _response = await _client.GetAsync("/");
            _html = await _response.Content.ReadAsStringAsync();
        }

        // SCRUM-128:
        // TEST 1: Verify expand controls exist
        [Then("the Home recent reports should include expand controls")]
        public void ThenTheHomeRecentReportsShouldIncludeExpandControls()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(CountOccurrences(_html, "home-report-toggle"), Is.EqualTo(2));
            Assert.That(CountOccurrences(_html, "aria-controls=\"home-report-details-"), Is.EqualTo(2));
            Assert.That(_html, Does.Contain("Expand"));
        }

        // SCRUM-128:
        // TEST 2: Verify hidden details panels exist
        [Then("the Home recent reports should include hidden inline details panels")]
        public void ThenTheHomeRecentReportsShouldIncludeHiddenInlineDetailsPanels()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(CountOccurrences(_html, "id=\"home-report-details-"), Is.EqualTo(2));
            Assert.That(CountOccurrences(_html, "class=\"home-report-details"), Is.EqualTo(2));
            Assert.That(CountOccurrences(_html, "hidden"), Is.GreaterThanOrEqualTo(2));
            Assert.That(_html, Does.Contain("Description:"));
            Assert.That(_html, Does.Contain("Reported:"));
            Assert.That(_html, Does.Contain("Status:"));
        }

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;

            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
