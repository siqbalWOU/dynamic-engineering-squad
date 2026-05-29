using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Dashboard
{
    // SCRUM-143: Tests private Dashboard activity progress labels.
    [TestFixture]
    public class DashboardActivityProgressRepositoryTests
    {
        private ApplicationDbContext _db = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("DashboardActivityProgressRepositoryTest_" + Guid.NewGuid())
                .Options;

            _db = new ApplicationDbContext(options);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        // TEST 1: Zero reports returns New Reporter.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenUserHasZeroReports_ReturnsNewReporter()
        {
            // Arrange: create a logged-in user with no submitted reports.
            var currentUser = CreateUser("current-user", "current@test.com");
            _db.Users.Add(currentUser);
            await _db.SaveChangesAsync();
            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: the exact activity progress label matches the zero-report threshold.
            Assert.That(result.ReportActivityProgressLabel, Is.EqualTo("New Reporter"));
        }

        // TEST 2: One report returns Getting Started.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenUserHasOneReport_ReturnsGettingStarted()
        {
            // Arrange: create a logged-in user with one submitted report.
            var currentUser = CreateUser("current-user", "current@test.com");
            _db.Users.Add(currentUser);
            await AddReportsForUserAsync(currentUser.Id, 1);
            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: the exact activity progress label matches the one-report threshold.
            Assert.That(result.ReportActivityProgressLabel, Is.EqualTo("Getting Started"));
        }

        // TEST 3: Nine reports returns Getting Started.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenUserHasNineReports_ReturnsGettingStarted()
        {
            // Arrange: create a logged-in user with nine submitted reports.
            var currentUser = CreateUser("current-user", "current@test.com");
            _db.Users.Add(currentUser);
            await AddReportsForUserAsync(currentUser.Id, 9);
            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: the exact activity progress label matches the upper Getting Started threshold.
            Assert.That(result.ReportActivityProgressLabel, Is.EqualTo("Getting Started"));
        }

        // TEST 4: Ten reports returns Active Reporter.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenUserHasTenReports_ReturnsActiveReporter()
        {
            // Arrange: create a logged-in user with ten submitted reports.
            var currentUser = CreateUser("current-user", "current@test.com");
            _db.Users.Add(currentUser);
            await AddReportsForUserAsync(currentUser.Id, 10);
            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: the exact activity progress label matches the lower Active Reporter threshold.
            Assert.That(result.ReportActivityProgressLabel, Is.EqualTo("Active Reporter"));
        }

        // TEST 5: Twenty-four reports returns Active Reporter.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenUserHasTwentyFourReports_ReturnsActiveReporter()
        {
            // Arrange: create a logged-in user with twenty-four submitted reports.
            var currentUser = CreateUser("current-user", "current@test.com");
            _db.Users.Add(currentUser);
            await AddReportsForUserAsync(currentUser.Id, 24);
            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: the exact activity progress label matches the upper Active Reporter threshold.
            Assert.That(result.ReportActivityProgressLabel, Is.EqualTo("Active Reporter"));
        }

        // TEST 6: Twenty-five reports returns Community Contributor.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenUserHasTwentyFiveReports_ReturnsCommunityContributor()
        {
            // Arrange: create a logged-in user with twenty-five submitted reports.
            var currentUser = CreateUser("current-user", "current@test.com");
            _db.Users.Add(currentUser);
            await AddReportsForUserAsync(currentUser.Id, 25);
            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: the exact activity progress label matches the Community Contributor threshold.
            Assert.That(result.ReportActivityProgressLabel, Is.EqualTo("Community Contributor"));
        }

        // TEST 7: Other users' reports do not affect the logged-in user's activity progress label.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenOtherUserHasTwentyFiveReports_ReturnsCurrentUsersLabel()
        {
            // Arrange: create a logged-in user with no reports and another user with many reports.
            var currentUser = CreateUser("current-user", "current@test.com");
            var otherUser = CreateUser("other-user", "other@test.com");
            _db.Users.AddRange(currentUser, otherUser);
            await AddReportsForUserAsync(otherUser.Id, 25);
            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: only the logged-in user's report count determines the exact label.
            Assert.That(result.ReportActivityProgressLabel, Is.EqualTo("New Reporter"));
        }

        private DashboardRepositoryEf CreateRepositoryForCurrentUser(Users currentUser)
        {
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, currentUser.Id)
                }, "TestAuth"))
            };

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);

            var userManager = CreateUserManager(currentUser);
            Mock.Get(userManager)
                .Setup(m => m.GetUserAsync(httpContext.User))
                .ReturnsAsync(currentUser);

            return new DashboardRepositoryEf(_db, userManager, httpContextAccessor.Object);
        }

        private static UserManager<Users> CreateUserManager(Users currentUser)
        {
            var store = new Mock<IUserStore<Users>>();
            var userManager = new Mock<UserManager<Users>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManager.Setup(m => m.FindByIdAsync(currentUser.Id))
                .ReturnsAsync(currentUser);

            return userManager.Object;
        }

        private static Users CreateUser(string userName, string email)
        {
            return new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                Email = email,
                EmailConfirmed = true
            };
        }

        private async Task AddReportsForUserAsync(string userId, int reportCount)
        {
            for (var i = 0; i < reportCount; i++)
            {
                _db.ReportIssue.Add(new ReportIssue
                {
                    UserId = userId,
                    Description = $"SCRUM-143 activity report {i + 1}",
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            await _db.SaveChangesAsync();
        }
    }
}
