using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Leaderboard
{
    [TestFixture]
    public class LeaderboardRepositoryTests
    {
        private ApplicationDbContext _db = null!;
        private LeaderboardRepositoryEf _repo = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("LeaderboardRepoTest_" + Guid.NewGuid())
                .Options;

            _db = new ApplicationDbContext(options);
            _repo = new LeaderboardRepositoryEf(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        private async Task<Users> AddUserAsync(string username)
        {
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = username,
                Email = $"{username}@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        private async Task AddPointsAsync(string userId, int points)
        {
            _db.UserPoints.Add(new UserPoints
            {
                UserId = userId,
                CurrentPoints = points,
                LifetimePoints = points,
                LastUpdated = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        // ── Basic retrieval ───────────────────────────────────────────────────

        [Test]
        public async Task GetAllAsync_WhenNoUsers_ReturnsEmptyCollection()
        {
            var result = await _repo.GetAllAsync();

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetAllAsync_ReturnsOneEntryPerUser()
        {
            await AddUserAsync("alice");
            await AddUserAsync("bob");

            var result = await _repo.GetAllAsync();

            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetAllAsync_ReturnsCorrectUsername()
        {
            await AddUserAsync("julian");

            var result = await _repo.GetAllAsync();

            Assert.That(result.First().UserId, Is.EqualTo("julian"));
        }

        [Test]
        public async Task GetAllAsync_ReturnsCorrectPoints_WhenUserHasPoints()
        {
            var user = await AddUserAsync("erin");
            await AddPointsAsync(user.Id, 150);

            var result = await _repo.GetAllAsync();

            Assert.That(result.First().UserPoints, Is.EqualTo(150));
        }

        [Test]
        public async Task GetAllAsync_ReturnsZeroPoints_WhenUserHasNoPointsRecord()
        {
            await AddUserAsync("phillip");

            var result = await _repo.GetAllAsync();

            Assert.That(result.First().UserPoints, Is.EqualTo(0));
        }

        [Test]
        public async Task GetAllAsync_ReturnsReadOnlyCollection()
        {
            var result = await _repo.GetAllAsync();

            Assert.That(result, Is.InstanceOf<IReadOnlyCollection<LeaderboardEntry>>());
        }

        // ── Null username filtering ──────────────────────────────────────────

        [Test]
        public async Task GetAllAsync_ExcludesUsers_WithNullUsername()
        {
            // Add a user with null username directly (bypassing Identity validation)
            _db.Users.Add(new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = null,
                Email = "noname@test.com"
            });
            await _db.SaveChangesAsync();

            var result = await _repo.GetAllAsync();

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetAllAsync_IncludesUsers_WithValidUsername_AndExcludesNullUsername()
        {
            await AddUserAsync("sunair");

            _db.Users.Add(new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = null,
                Email = "ghost@test.com"
            });
            await _db.SaveChangesAsync();

            var result = await _repo.GetAllAsync();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().UserId, Is.EqualTo("sunair"));
        }

        // ── Multiple users with points ────────────────────────────────────────

        [Test]
        public async Task GetAllAsync_ReturnsCorrectPointsForMultipleUsers()
        {
            var userA = await AddUserAsync("userA");
            var userB = await AddUserAsync("userB");
            await AddPointsAsync(userA.Id, 100);
            await AddPointsAsync(userB.Id, 200);

            var result = await _repo.GetAllAsync();

            var points = result.Select(e => e.UserPoints).ToList();
            Assert.That(points, Does.Contain(100));
            Assert.That(points, Does.Contain(200));
        }
    }
}
