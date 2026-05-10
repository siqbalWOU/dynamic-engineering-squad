//tests to see if users appear in report details page
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InfrastructureApp_Tests.Integration;

[TestFixture]
public class ReportIssueDetailsViewIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbName = null!;

    [SetUp]
    public void SetUp()
    {
        _dbName = "ReportIssueDetailsView_" + Guid.NewGuid().ToString("N");

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

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Details_RendersReporterUsername()
    {
        int reportId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = new Users
            {
                Id = "report-user-1",
                UserName = "reporter1",
                NormalizedUserName = "REPORTER1",
                Email = "reporter1@test.local",
                NormalizedEmail = "REPORTER1@TEST.LOCAL"
            };

            db.Users.Add(user);

            var report = new InfrastructureApp.Models.ReportIssue
            {
                Description = "Broken curb by school",
                Status = "Approved",
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id
            };

            db.ReportIssue.Add(report);
            await db.SaveChangesAsync();
            reportId = report.Id;
        }

        var response = await _client.GetAsync($"/ReportIssue/Details/{reportId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(html, Does.Contain("Reported by:</strong> reporter1"));
    }
}
