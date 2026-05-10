using System;
using System.Linq;
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
    // SCRUM-137: Tests private Dashboard retrieval for the logged-in user's submitted reports.
    [TestFixture]
    public class DashboardSubmittedReportsRepositoryTests
    {
        private ApplicationDbContext _db = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("DashboardSubmittedReportsRepositoryTest_" + Guid.NewGuid())
                .Options;

            _db = new ApplicationDbContext(options);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        // TEST 1: The private Dashboard includes reports submitted by the logged-in user.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenCurrentUserHasReports_ReturnsSubmittedReports()
        {
            // Arrange: create a logged-in user with one submitted report.
            var currentUser = CreateUser("current-user", "current@test.com");
            var report = CreateReport(currentUser.Id, "Broken sidewalk", "Approved", new DateTime(2026, 5, 1));
            _db.Users.Add(currentUser);
            _db.ReportIssue.Add(report);
            await _db.SaveChangesAsync();

            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: the submitted report appears in the Dashboard list.
            Assert.That(result.SubmittedReports, Has.Count.EqualTo(1));
            Assert.That(result.SubmittedReports[0].Id, Is.EqualTo(report.Id));
            Assert.That(result.SubmittedReports[0].Description, Is.EqualTo("Broken sidewalk"));
            Assert.That(result.SubmittedReports[0].Status, Is.EqualTo("Approved"));
            Assert.That(result.SubmittedReports[0].CreatedDate, Is.EqualTo(new DateTime(2026, 5, 1)));
        }

        // TEST 2: The private Dashboard excludes reports submitted by other users.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenOtherUsersHaveReports_ExcludesOtherUsersReports()
        {
            // Arrange: create reports for the logged-in user and another user.
            var currentUser = CreateUser("current-user", "current@test.com");
            var otherUser = CreateUser("other-user", "other@test.com");
            var currentUserReport = CreateReport(currentUser.Id, "Current user report", "Pending", new DateTime(2026, 5, 2));
            var otherUserReport = CreateReport(otherUser.Id, "Other user report", "Approved", new DateTime(2026, 5, 3));
            _db.Users.AddRange(currentUser, otherUser);
            _db.ReportIssue.AddRange(currentUserReport, otherUserReport);
            await _db.SaveChangesAsync();

            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: only the logged-in user's report appears.
            Assert.That(result.SubmittedReports, Has.Count.EqualTo(1));
            Assert.That(result.SubmittedReports[0].Id, Is.EqualTo(currentUserReport.Id));
            Assert.That(result.SubmittedReports.Select(r => r.Id), Does.Not.Contain(otherUserReport.Id));
            Assert.That(result.SubmittedReports.Select(r => r.Description), Does.Not.Contain("Other user report"));
        }

        // TEST 3: The private Dashboard orders submitted reports newest first.
        [Test]
        public async Task GetDashboardSummaryAsync_WhenCurrentUserHasMultipleReports_OrdersNewestFirst()
        {
            // Arrange: create multiple reports for the logged-in user with different dates.
            var currentUser = CreateUser("current-user", "current@test.com");
            var olderReport = CreateReport(currentUser.Id, "Older report", "Approved", new DateTime(2026, 4, 1));
            var newestReport = CreateReport(currentUser.Id, "Newest report", "Pending", new DateTime(2026, 5, 5));
            var middleReport = CreateReport(currentUser.Id, "Middle report", "Rejected", new DateTime(2026, 4, 15));
            _db.Users.Add(currentUser);
            _db.ReportIssue.AddRange(olderReport, newestReport, middleReport);
            await _db.SaveChangesAsync();

            var repo = CreateRepositoryForCurrentUser(currentUser);

            // Act: load the private Dashboard summary.
            var result = await repo.GetDashboardSummaryAsync();

            // Assert: reports are sorted by created date descending.
            Assert.That(result.SubmittedReports.Select(r => r.Description).ToList(), Is.EqualTo(new[]
            {
                "Newest report",
                "Middle report",
                "Older report"
            }));
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

        private static ReportIssue CreateReport(string userId, string description, string status, DateTime createdAt)
        {
            return new ReportIssue
            {
                UserId = userId,
                Description = description,
                Status = status,
                CreatedAt = createdAt
            };
        }
    }
}
