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
    public class LeaderboardSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;
        private HttpResponseMessage _response = null!;
        private string _html = string.Empty;
        private readonly string _dbName;

        public LeaderboardSteps()
        {
            _dbName = "LeaderboardTest_" + Guid.NewGuid();

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
                        options.UseInMemoryDatabase(_dbName));
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true
            });
        }

        [Given("I navigate to the Leaderboard page")]
        [When("I navigate to the Leaderboard page")]
        public async Task GivenINavigateToTheLeaderboardPage()
        {
            _response = await _client.GetAsync("/Leaderboard");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Given("I navigate to the Home page as a visitor")]
        public async Task GivenINavigateToTheHomePageAsAVisitor()
        {
            _response = await _client.GetAsync("/");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Given("no leaderboard entries exist")]
        public async Task GivenNoLeaderboardEntriesExist()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.RemoveRange(db.Users);
            db.UserPoints.RemoveRange(db.UserPoints);
            await db.SaveChangesAsync();
        }

        [Given("the following leaderboard entries exist")]
        public async Task GivenTheFollowingLeaderboardEntriesExist(DataTable table)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            foreach (var row in table.Rows)
            {
                var userId = Guid.NewGuid().ToString();
                db.Users.Add(new Users
                {
                    Id = userId,
                    UserName = row["UserId"],
                    Email = $"{row["UserId"]}@test.com",
                    EmailConfirmed = true
                });
                db.UserPoints.Add(new UserPoints
                {
                    UserId = userId,
                    CurrentPoints = int.Parse(row["UserPoints"]),
                    LifetimePoints = int.Parse(row["UserPoints"]),
                    LastUpdated = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }

        [Then("the leaderboard response should be 200 OK")]
        public void ThenTheLeaderboardResponseShouldBe200OK()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Then("the leaderboard page should contain {string}")]
        public void ThenTheLeaderboardPageShouldContain(string expectedText)
        {
            Assert.That(_html, Does.Contain(expectedText));
        }

        [Then("the leaderboard home page should contain a link to the Leaderboard")]
        public void ThenTheHomePageShouldContainALinkToTheLeaderboard()
        {
            Assert.That(_html, Does.Contain("/Leaderboard"));
        }

        [Then("{string} should appear before {string} on the leaderboard page")]
        public void ThenUserShouldAppearBeforeOtherUser(string firstUser, string secondUser)
        {
            var firstIndex = _html.IndexOf(firstUser, StringComparison.Ordinal);
            var secondIndex = _html.IndexOf(secondUser, StringComparison.Ordinal);

            Assert.That(firstIndex, Is.GreaterThan(-1), $"'{firstUser}' not found on page");
            Assert.That(secondIndex, Is.GreaterThan(-1), $"'{secondUser}' not found on page");
            Assert.That(firstIndex, Is.LessThan(secondIndex),
                $"Expected '{firstUser}' to appear before '{secondUser}'");
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
