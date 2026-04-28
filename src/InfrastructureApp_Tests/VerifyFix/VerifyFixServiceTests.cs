using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;

namespace InfrastructureApp_Tests.VerifyFix
{
    [TestFixture]
    public class VerifyFixServiceTests
    {
        private ApplicationDbContext _db = null!;
        private VerifyFixService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("VerifyFixServiceTest_" + Guid.NewGuid())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _db = new ApplicationDbContext(options);
            _service = new VerifyFixService(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        private async Task<ReportIssue> AddReportAsync(string status = "Approved")
        {
            var report = new ReportIssue
            {
                Description = "Test report",
                Status = status,
                UserId = "owner-user",
                CreatedAt = DateTime.UtcNow
            };
            _db.ReportIssue.Add(report);
            await _db.SaveChangesAsync();
            return report;
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

        // ── GetVerifyStatusAsync ──────────────────────────────────────────────

        [Test]
        public async Task GetVerifyStatusAsync_WhenNoVerifications_ReturnsZeroCount()
        {
            var (verifyCount, _) = await _service.GetVerifyStatusAsync(1, null);

            Assert.That(verifyCount, Is.EqualTo(0));
        }

        [Test]
        public async Task GetVerifyStatusAsync_WhenNoVerifications_UserHasVerifiedIsFalse()
        {
            var (_, userHasVerified) = await _service.GetVerifyStatusAsync(1, "some-user");

            Assert.That(userHasVerified, Is.False);
        }

        [Test]
        public async Task GetVerifyStatusAsync_WhenNullUserId_UserHasVerifiedIsFalse()
        {
            var (_, userHasVerified) = await _service.GetVerifyStatusAsync(1, null);

            Assert.That(userHasVerified, Is.False);
        }

        [Test]
        public async Task GetVerifyStatusAsync_AfterVerification_ReturnsCorrectCount()
        {
            var report = await AddReportAsync();
            var user = await AddUserAsync("alice");
            await _service.ToggleVerificationAsync(report.Id, user.Id);

            var (verifyCount, _) = await _service.GetVerifyStatusAsync(report.Id, user.Id);

            Assert.That(verifyCount, Is.EqualTo(1));
        }

        [Test]
        public async Task GetVerifyStatusAsync_AfterVerification_UserHasVerifiedIsTrue()
        {
            var report = await AddReportAsync();
            var user = await AddUserAsync("alice");
            await _service.ToggleVerificationAsync(report.Id, user.Id);

            var (_, userHasVerified) = await _service.GetVerifyStatusAsync(report.Id, user.Id);

            Assert.That(userHasVerified, Is.True);
        }

        [Test]
        public async Task GetVerifyStatusAsync_OtherUserVerified_UserHasVerifiedIsFalse()
        {
            var report = await AddReportAsync();
            var alice = await AddUserAsync("alice");
            var bob = await AddUserAsync("bob");
            await _service.ToggleVerificationAsync(report.Id, alice.Id);

            var (_, userHasVerified) = await _service.GetVerifyStatusAsync(report.Id, bob.Id);

            Assert.That(userHasVerified, Is.False);
        }

        // ── ToggleVerificationAsync — adding ──────────────────────────────────

        [Test]
        public async Task ToggleVerificationAsync_FirstVerification_UserHasVerifiedIsTrue()
        {
            var report = await AddReportAsync();
            var user = await AddUserAsync("alice");

            var (_, userHasVerified) = await _service.ToggleVerificationAsync(report.Id, user.Id);

            Assert.That(userHasVerified, Is.True);
        }

        [Test]
        public async Task ToggleVerificationAsync_FirstVerification_CountIsOne()
        {
            var report = await AddReportAsync();
            var user = await AddUserAsync("alice");

            var (verifyCount, _) = await _service.ToggleVerificationAsync(report.Id, user.Id);

            Assert.That(verifyCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ToggleVerificationAsync_FirstVerification_AwardsXpToUser()
        {
            var report = await AddReportAsync();
            var user = await AddUserAsync("alice");

            await _service.ToggleVerificationAsync(report.Id, user.Id);

            var points = await _db.UserPoints.FirstAsync(p => p.UserId == user.Id);
            Assert.That(points.CurrentPoints, Is.EqualTo(3));
            Assert.That(points.LifetimePoints, Is.EqualTo(3));
        }

        // ── ToggleVerificationAsync — removing ────────────────────────────────

        [Test]
        public async Task ToggleVerificationAsync_SecondToggle_RemovesVerification()
        {
            var report = await AddReportAsync();
            var user = await AddUserAsync("alice");
            await _service.ToggleVerificationAsync(report.Id, user.Id);

            var (_, userHasVerified) = await _service.ToggleVerificationAsync(report.Id, user.Id);

            Assert.That(userHasVerified, Is.False);
        }

        [Test]
        public async Task ToggleVerificationAsync_SecondToggle_CountReturnsToZero()
        {
            var report = await AddReportAsync();
            var user = await AddUserAsync("alice");
            await _service.ToggleVerificationAsync(report.Id, user.Id);

            var (verifyCount, _) = await _service.ToggleVerificationAsync(report.Id, user.Id);

            Assert.That(verifyCount, Is.EqualTo(0));
        }

        // ── ToggleVerificationAsync — auto-transition ─────────────────────────

        [Test]
        public async Task ToggleVerificationAsync_ThreeVerificationsOnApprovedReport_TransitionsToResolved()
        {
            var report = await AddReportAsync("Approved");
            var alice = await AddUserAsync("alice");
            var bob = await AddUserAsync("bob");
            var charlie = await AddUserAsync("charlie");

            await _service.ToggleVerificationAsync(report.Id, alice.Id);
            await _service.ToggleVerificationAsync(report.Id, bob.Id);
            await _service.ToggleVerificationAsync(report.Id, charlie.Id);

            var updated = await _db.ReportIssue.FindAsync(report.Id);
            Assert.That(updated!.Status, Is.EqualTo("Resolved"));
        }

        [Test]
        public async Task ToggleVerificationAsync_TwoVerificationsOnApprovedReport_StatusRemainsApproved()
        {
            var report = await AddReportAsync("Approved");
            var alice = await AddUserAsync("alice");
            var bob = await AddUserAsync("bob");

            await _service.ToggleVerificationAsync(report.Id, alice.Id);
            await _service.ToggleVerificationAsync(report.Id, bob.Id);

            var updated = await _db.ReportIssue.FindAsync(report.Id);
            Assert.That(updated!.Status, Is.EqualTo("Approved"));
        }

        [Test]
        public async Task ToggleVerificationAsync_ThreeVerificationsOnNonApprovedReport_DoesNotTransition()
        {
            var report = await AddReportAsync("Pending");
            var alice = await AddUserAsync("alice");
            var bob = await AddUserAsync("bob");
            var charlie = await AddUserAsync("charlie");

            await _service.ToggleVerificationAsync(report.Id, alice.Id);
            await _service.ToggleVerificationAsync(report.Id, bob.Id);
            await _service.ToggleVerificationAsync(report.Id, charlie.Id);

            var updated = await _db.ReportIssue.FindAsync(report.Id);
            Assert.That(updated!.Status, Is.EqualTo("Pending"));
        }

        [Test]
        public async Task ToggleVerificationAsync_VerificationOnDifferentReport_DoesNotAffectOtherReport()
        {
            var report1 = await AddReportAsync("Approved");
            var report2 = await AddReportAsync("Approved");
            var user = await AddUserAsync("alice");

            await _service.ToggleVerificationAsync(report1.Id, user.Id);

            var (verifyCount, _) = await _service.GetVerifyStatusAsync(report2.Id, user.Id);
            Assert.That(verifyCount, Is.EqualTo(0));
        }
    }
}
