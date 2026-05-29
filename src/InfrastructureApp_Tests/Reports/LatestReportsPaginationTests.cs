using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Reports
{
    // SCRUM-157: NUnit coverage for Latest Reports server-side pagination.
    [TestFixture]
    public class LatestReportsPaginationTests
    {
        private SqliteConnection _connection = null!;
        private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var db = NewDb();
            db.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Dispose();
        }

        // TEST 1: First page returns only the configured page size.
        [Test]
        public async Task GetPaginatedLatestReportsAsync_FirstPage_ReturnsOnlyPageSizeCount()
        {
            // Arrange
            using var db = NewDb();
            await AddUserAsync(db, "user-1");
            await SeedReportsAsync(db, count: 15, userId: "user-1");
            var repo = new ReportIssueRepositoryEf(db);

            // Act
            var result = await repo.GetPaginatedLatestReportsAsync(isAdmin: false, keyword: null, sort: "newest", pageNumber: 1, pageSize: 10);

            // Assert
            Assert.That(result.Count, Is.EqualTo(10));
            Assert.That(result.PageIndex, Is.EqualTo(1));
            Assert.That(result.TotalPages, Is.EqualTo(2));
            Assert.That(result.HasNextPage, Is.True);
            Assert.That(result.HasPreviousPage, Is.False);
        }

        // TEST 2: Second page returns the next set of reports.
        [Test]
        public async Task GetPaginatedLatestReportsAsync_SecondPage_ReturnsNextSetOfReports()
        {
            // Arrange
            using var db = NewDb();
            await AddUserAsync(db, "user-2");
            await SeedReportsAsync(db, count: 15, userId: "user-2");
            var repo = new ReportIssueRepositoryEf(db);

            // Act
            var result = await repo.GetPaginatedLatestReportsAsync(isAdmin: false, keyword: null, sort: "newest", pageNumber: 2, pageSize: 10);

            // Assert
            Assert.That(result.Count, Is.EqualTo(5));
            Assert.That(result.Select(r => r.Description).ToList(), Is.EqualTo(new[]
            {
                "Report 05",
                "Report 04",
                "Report 03",
                "Report 02",
                "Report 01"
            }));
            Assert.That(result.PageIndex, Is.EqualTo(2));
            Assert.That(result.HasNextPage, Is.False);
            Assert.That(result.HasPreviousPage, Is.True);
        }

        // TEST 3: Newest sort stays correct across pages.
        [Test]
        public async Task GetPaginatedLatestReportsAsync_NewestSort_OrdersCorrectlyAcrossPages()
        {
            // Arrange
            using var db = NewDb();
            await AddUserAsync(db, "user-3");
            await SeedReportsAsync(db, count: 6, userId: "user-3");
            var repo = new ReportIssueRepositoryEf(db);

            // Act
            var pageOne = await repo.GetPaginatedLatestReportsAsync(isAdmin: false, keyword: null, sort: "newest", pageNumber: 1, pageSize: 3);
            var pageTwo = await repo.GetPaginatedLatestReportsAsync(isAdmin: false, keyword: null, sort: "newest", pageNumber: 2, pageSize: 3);

            // Assert
            Assert.That(pageOne.Select(r => r.Description).ToList(), Is.EqualTo(new[] { "Report 06", "Report 05", "Report 04" }));
            Assert.That(pageTwo.Select(r => r.Description).ToList(), Is.EqualTo(new[] { "Report 03", "Report 02", "Report 01" }));
        }

        // TEST 4: Oldest sort stays correct across pages.
        [Test]
        public async Task GetPaginatedLatestReportsAsync_OldestSort_OrdersCorrectlyAcrossPages()
        {
            // Arrange
            using var db = NewDb();
            await AddUserAsync(db, "user-4");
            await SeedReportsAsync(db, count: 6, userId: "user-4");
            var repo = new ReportIssueRepositoryEf(db);

            // Act
            var pageOne = await repo.GetPaginatedLatestReportsAsync(isAdmin: false, keyword: null, sort: "oldest", pageNumber: 1, pageSize: 3);
            var pageTwo = await repo.GetPaginatedLatestReportsAsync(isAdmin: false, keyword: null, sort: "oldest", pageNumber: 2, pageSize: 3);

            // Assert
            Assert.That(pageOne.Select(r => r.Description).ToList(), Is.EqualTo(new[] { "Report 01", "Report 02", "Report 03" }));
            Assert.That(pageTwo.Select(r => r.Description).ToList(), Is.EqualTo(new[] { "Report 04", "Report 05", "Report 06" }));
        }

        // TEST 5: Search filters before pagination.
        [Test]
        public async Task GetPaginatedLatestReportsAsync_SearchFilter_AppliesBeforePagination()
        {
            // Arrange
            using var db = NewDb();
            await AddUserAsync(db, "user-5");
            await SeedReportsAsync(db, count: 12, userId: "user-5", descriptionPrefix: "Pothole");
            await SeedReportsAsync(db, count: 5, userId: "user-5", descriptionPrefix: "Streetlight");
            var repo = new ReportIssueRepositoryEf(db);

            // Act
            var pageOne = await repo.GetPaginatedLatestReportsAsync(isAdmin: false, keyword: "Pothole", sort: "newest", pageNumber: 1, pageSize: 10);
            var pageTwo = await repo.GetPaginatedLatestReportsAsync(isAdmin: false, keyword: "Pothole", sort: "newest", pageNumber: 2, pageSize: 10);

            // Assert
            Assert.That(pageOne.Count, Is.EqualTo(10));
            Assert.That(pageTwo.Count, Is.EqualTo(2));
            Assert.That(pageOne.TotalPages, Is.EqualTo(2));
            Assert.That(pageOne.All(r => r.Description.Contains("Pothole")), Is.True);
            Assert.That(pageTwo.All(r => r.Description.Contains("Pothole")), Is.True);
        }

        // TEST 6: Non-admin users only see approved reports.
        [Test]
        public async Task GetPaginatedLatestReportsAsync_WhenNotAdmin_ReturnsOnlyApprovedReports()
        {
            // Arrange
            using var db = NewDb();
            await AddUserAsync(db, "user-6");
            await SeedReportsAsync(db, count: 2, userId: "user-6", descriptionPrefix: "Approved", status: "Approved");
            await SeedReportsAsync(db, count: 2, userId: "user-6", descriptionPrefix: "Pending", status: "Pending");
            await SeedReportsAsync(db, count: 1, userId: "user-6", descriptionPrefix: "Rejected", status: "Rejected");
            var repo = new ReportIssueRepositoryEf(db);

            // Act
            var result = await repo.GetPaginatedLatestReportsAsync(isAdmin: false, keyword: null, sort: "newest", pageNumber: 1, pageSize: 10);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(r => r.Status == "Approved"), Is.True);
            Assert.That(result.Select(r => r.Description).ToList(), Is.EqualTo(new[] { "Approved 02", "Approved 01" }));
        }

        private ApplicationDbContext NewDb()
        {
            return new ApplicationDbContext(_dbOptions);
        }

        private static async Task AddUserAsync(ApplicationDbContext db, string userId)
        {
            db.Users.Add(new Users
            {
                Id = userId,
                UserName = userId,
                NormalizedUserName = userId.ToUpperInvariant(),
                Email = $"{userId}@test.local",
                NormalizedEmail = $"{userId}@test.local".ToUpperInvariant()
            });

            await db.SaveChangesAsync();
        }

        private static async Task SeedReportsAsync(ApplicationDbContext db, int count, string userId, string descriptionPrefix = "Report", string status = "Approved")
        {
            var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 1; i <= count; i++)
            {
                db.ReportIssue.Add(new ReportIssue
                {
                    Description = $"{descriptionPrefix} {i:00}",
                    Status = status,
                    CreatedAt = startDate.AddMinutes(i),
                    UserId = userId,
                    Latitude = 44.85m,
                    Longitude = -123.23m
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
