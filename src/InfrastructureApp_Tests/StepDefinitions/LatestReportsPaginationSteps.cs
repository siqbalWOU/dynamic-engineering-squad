using System.Net;
using System.Text.RegularExpressions;
using InfrastructureApp.Data;
using InfrastructureApp_Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    // SCRUM-157: BDD steps for Latest Reports server-side pagination.
    [Binding]
    public class LatestReportsPaginationSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _dbName;
        private HttpClient _client = null!;
        private HttpResponseMessage _response = null!;
        private string _html = string.Empty;

        public LatestReportsPaginationSteps()
        {
            _dbName = "LatestReportsPaginationTest_" + Guid.NewGuid();

            // Each scenario gets an isolated in-memory database with the normal app services.
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

                    // Replace the app database so seeded reports are scoped to this feature scenario.
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

        // GIVEN steps seed enough approved reports to exercise two server-side pages.
        [Given("more than one page of latest reports exists")]
        public async Task GivenMoreThanOnePageOfLatestReportsExists()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await SeedLatestReportsAsync(db, count: 15, descriptionPrefix: "Latest report");
        }

        // Search scenarios use matching and non-matching descriptions to prove filtering happens first.
        [Given("more than one page of searchable latest reports exists")]
        public async Task GivenMoreThanOnePageOfSearchableLatestReportsExists()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await SeedLatestReportsAsync(db, count: 12, descriptionPrefix: "Pothole");
            await SeedLatestReportsAsync(db, count: 4, descriptionPrefix: "Streetlight");
        }

        // WHEN steps request the same URLs a browser would use from the Latest Reports UI.
        [When("I visit the Latest Reports page")]
        public async Task WhenIVisitTheLatestReportsPage()
        {
            await LoadPageAsync("/Reports/Latest");
        }

        [When("I go to the next Latest Reports page")]
        public async Task WhenIGoToTheNextLatestReportsPage()
        {
            await LoadPageAsync("/Reports/Latest?page=2");
        }

        [When("I search Latest Reports for {string}")]
        public async Task WhenISearchLatestReportsFor(string searchTerm)
        {
            await LoadPageAsync($"/Reports/Latest?page=1&query={Uri.EscapeDataString(searchTerm)}&sort=newest");
        }

        [When("I sort Latest Reports by oldest first")]
        public async Task WhenISortLatestReportsByOldestFirst()
        {
            await LoadPageAsync("/Reports/Latest?page=1&sort=oldest");
        }

        [When("I open a report from the Latest Reports list")]
        public void WhenIOpenAReportFromTheLatestReportsList()
        {
            // Server-side BDD checks the markup required for the existing modal JavaScript to open.
            Assert.That(_html, Does.Contain("data-testid=\"latest-report-item\""));
            Assert.That(_html, Does.Contain("data-bs-target=\"#reportModal\""));
        }

        // THEN steps assert the rendered HTML state produced by server-side pagination.
        [Then("I should see Latest Reports pagination controls")]
        public void ThenIShouldSeeLatestReportsPaginationControls()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            // The nav label and Previous/Next text are the user-visible pagination controls.
            Assert.That(_html, Does.Contain("aria-label=\"Latest Reports pagination\""));
            Assert.That(_html, Does.Contain("Previous"));
            Assert.That(_html, Does.Contain("Next"));
        }

        [Then("the first page should be marked as the current page")]
        public void ThenTheFirstPageShouldBeMarkedAsTheCurrentPage()
        {
            AssertCurrentPageIs("1");
        }

        [Then("I should see the second page of Latest Reports")]
        public void ThenIShouldSeeTheSecondPageOfLatestReports()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_html, Does.Contain("Latest report 05"));
            Assert.That(_html, Does.Not.Contain("Latest report 15"));
        }

        [Then("the second page should be marked as the current page")]
        public void ThenTheSecondPageShouldBeMarkedAsTheCurrentPage()
        {
            AssertCurrentPageIs("2");
        }

        [Then("the pagination links should preserve the search term {string}")]
        public void ThenThePaginationLinksShouldPreserveTheSearchTerm(string searchTerm)
        {
            // Pagination links must carry the search term so page 2 stays filtered.
            Assert.That(_html, Does.Contain($"query={searchTerm}"));
        }

        [Then("I should see the oldest Latest Reports first")]
        public void ThenIShouldSeeTheOldestLatestReportsFirst()
        {
            // The first two oldest reports should appear in chronological order on page 1.
            var firstIndex = _html.IndexOf("Latest report 01", StringComparison.Ordinal);
            var secondIndex = _html.IndexOf("Latest report 02", StringComparison.Ordinal);

            Assert.That(firstIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(secondIndex, Is.GreaterThan(firstIndex));
        }

        [Then("the pagination links should preserve the oldest first sort")]
        public void ThenThePaginationLinksShouldPreserveTheOldestFirstSort()
        {
            // Pagination links must carry the sort value so page 2 keeps oldest-first order.
            Assert.That(_html, Does.Contain("sort=oldest"));
        }

        [Then("the Latest Reports modal should open for that report")]
        public void ThenTheLatestReportsModalShouldOpenForThatReport()
        {
            // This verifies the modal container/details markup still exists after pagination.
            Assert.That(_html, Does.Contain("id=\"reportModal\""));
            Assert.That(_html, Does.Contain("data-testid=\"report-modal\""));
            Assert.That(_html, Does.Contain("Report Details"));
        }

        private async Task LoadPageAsync(string path)
        {
            _response = await _client.GetAsync(path);
            _html = await _response.Content.ReadAsStringAsync();
        }

        private static async Task SeedLatestReportsAsync(ApplicationDbContext db, int count, string descriptionPrefix)
        {
            var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var userId = $"{descriptionPrefix.Replace(" ", "-").ToLowerInvariant()}-user";

            for (var i = 1; i <= count; i++)
            {
                await ReportIssueTestDataHelper.CreateTestReportAsync(
                    db,
                    $"{descriptionPrefix} {i:00}",
                    "Approved",
                    userId,
                    createdAt: startDate.AddMinutes(i),
                    latitude: 44.85m,
                    longitude: -123.23m);
            }
        }

        private void AssertCurrentPageIs(string pageNumber)
        {
            // Active page styling and aria-current make the current page obvious to users.
            var pattern = $@"<li[^>]*class=""[^""]*active[^""]*""[^>]*>\s*<a[^>]*aria-current=""page""[^>]*>\s*{pageNumber}\s*</a>";
            Assert.That(Regex.IsMatch(_html, pattern, RegexOptions.Singleline), Is.True);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
