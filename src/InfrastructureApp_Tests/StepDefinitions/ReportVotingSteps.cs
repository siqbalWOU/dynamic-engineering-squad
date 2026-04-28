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
    public class ReportVotingSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;
        private HttpResponseMessage _response = null!;
        private string _html = string.Empty;
        private int _lastReportId;
        private readonly string _dbName;

        public ReportVotingSteps()
        {
            _dbName = "VoteTest_" + Guid.NewGuid();

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
                AllowAutoRedirect = true
            });
        }

        [Given("a report exists with description {string}")]
        public async Task GivenAReportExistsWithDescription(string description)
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

        [When("I navigate to that report's details page")]
        public async Task WhenINavigateToThatReportsDetailsPage()
        {
            _response = await _client.GetAsync($"/ReportIssue/Details/{_lastReportId}");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [When("I request the vote status for that report")]
        public async Task WhenIRequestTheVoteStatusForThatReport()
        {
            _response = await _client.GetAsync($"/Vote/Status/{_lastReportId}");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [When("I navigate to the Latest Reports page")]
        public async Task WhenINavigateToTheLatestReportsPage()
        {
            _response = await _client.GetAsync("/Reports/Latest");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Then("the voting page should contain {string}")]
        public void ThenTheVotingPageShouldContain(string expectedText)
        {
            Assert.That(_html, Does.Contain(expectedText));
        }

        [Then("the vote status response should be 200 OK")]
        public void ThenTheVoteStatusResponseShouldBe200OK()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Then("the vote status should contain {string}")]
        public void ThenTheVoteStatusShouldContain(string expectedText)
        {
            Assert.That(_html, Does.Contain(expectedText));
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
