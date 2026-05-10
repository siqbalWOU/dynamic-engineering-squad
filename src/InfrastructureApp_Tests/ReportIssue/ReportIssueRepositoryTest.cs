/* this file is testing your EF Core repository against a real (but temporary) SQLite database that lives only in memory*/ 

using System;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Repositories
{
    [TestFixture]
    public class ReportIssueRepositoryTests
    {
        private SqliteConnection _conn = null!;
        private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

        [SetUp]
        public void SetUp()
        {
            // Create in-memory SQLite connection
            _conn = new SqliteConnection("Filename=:memory:");
            _conn.Open();

            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_conn)
                .Options;

            // Ensure schema is created
            using var db = new ApplicationDbContext(_dbOptions);
            db.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            _conn.Dispose();
        }

        private ApplicationDbContext NewDb()
            => new ApplicationDbContext(_dbOptions);

        private static async Task AddUserAsync(ApplicationDbContext db, string userId, string userName)
        {
            db.Users.Add(new Users
            {
                Id = userId,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = $"{userId}@test.local",
                NormalizedEmail = $"{userId}@test.local".ToUpperInvariant()
            });

            await db.SaveChangesAsync();
        }

        //tests SQL and EF
        [Test]
        public async Task AddAsync_PersistsReport_AndGetByIdAsync_ReturnsIt()
        {
            using var db = NewDb();
            var repo = new ReportIssueRepositoryEf(db);
            await AddUserAsync(db, "user-123", "reporter123");

            var report = new ReportIssue
            {
                Description = "Broken curb",
                Status = "Approved",
                CreatedAt = DateTime.UtcNow,
                UserId = "user-123",
                Latitude = 44.85m,
                Longitude = -123.23m,
                ImageUrl = "/uploads/issues/test.png",
                ImageSha256 = "1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
                ImagePHash = 444444444L
            };

            // Act: add and save
            await repo.AddAsync(report);
            await repo.SaveChangesAsync();

            var savedId = report.Id;
            Assert.That(savedId, Is.GreaterThan(0), "EF should generate an Id");

            // Act: fetch from repository
            var fetched = await repo.GetByIdAsync(savedId);

            // Assert
            Assert.That(fetched, Is.Not.Null);
            Assert.That(fetched!.Id, Is.EqualTo(savedId));
            Assert.That(fetched.Description, Is.EqualTo("Broken curb"));
            Assert.That(fetched.UserId, Is.EqualTo("user-123"));
            Assert.That(fetched.User, Is.Not.Null);
            Assert.That(fetched.User!.UserName, Is.EqualTo("reporter123"));
            Assert.That(fetched.Status, Is.EqualTo("Approved"));
            Assert.That(fetched.ImageUrl, Is.EqualTo("/uploads/issues/test.png"));
            Assert.That(fetched.ImageSha256,
                Is.EqualTo("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"));
            Assert.That(fetched.ImagePHash, Is.EqualTo(444444444L));
        }
    }
}
