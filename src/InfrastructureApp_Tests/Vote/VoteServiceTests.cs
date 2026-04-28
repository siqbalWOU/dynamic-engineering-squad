using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Vote
{
    [TestFixture]
    public class VoteServiceTests
    {
        private ApplicationDbContext _db = null!;
        private VoteService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("VoteServiceTest_" + Guid.NewGuid())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _db = new ApplicationDbContext(options);
            _service = new VoteService(_db);
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

        // ── GetVoteStatusAsync ────────────────────────────────────────────────

        [Test]
        public async Task GetVoteStatusAsync_WhenNoVotes_ReturnsZeroCount()
        {
            var (voteCount, _) = await _service.GetVoteStatusAsync(1, null);

            Assert.That(voteCount, Is.EqualTo(0));
        }

        [Test]
        public async Task GetVoteStatusAsync_WhenNoVotes_UserHasVotedIsFalse()
        {
            var (_, userHasVoted) = await _service.GetVoteStatusAsync(1, "some-user-id");

            Assert.That(userHasVoted, Is.False);
        }

        [Test]
        public async Task GetVoteStatusAsync_WhenNullUserId_UserHasVotedIsFalse()
        {
            var (_, userHasVoted) = await _service.GetVoteStatusAsync(1, null);

            Assert.That(userHasVoted, Is.False);
        }

        [Test]
        public async Task GetVoteStatusAsync_AfterVoteCast_ReturnsCorrectCount()
        {
            var user = await AddUserAsync("alice");
            await _service.ToggleVoteAsync(1, user.Id);

            var (voteCount, _) = await _service.GetVoteStatusAsync(1, user.Id);

            Assert.That(voteCount, Is.EqualTo(1));
        }

        [Test]
        public async Task GetVoteStatusAsync_AfterVoteCast_UserHasVotedIsTrue()
        {
            var user = await AddUserAsync("alice");
            await _service.ToggleVoteAsync(1, user.Id);

            var (_, userHasVoted) = await _service.GetVoteStatusAsync(1, user.Id);

            Assert.That(userHasVoted, Is.True);
        }

        [Test]
        public async Task GetVoteStatusAsync_OtherUserHasVoted_UserHasVotedIsFalse()
        {
            var alice = await AddUserAsync("alice");
            var bob = await AddUserAsync("bob");
            await _service.ToggleVoteAsync(1, alice.Id);

            var (_, userHasVoted) = await _service.GetVoteStatusAsync(1, bob.Id);

            Assert.That(userHasVoted, Is.False);
        }

        [Test]
        public async Task GetVoteStatusAsync_MultipleVoters_ReturnsCorrectCount()
        {
            var alice = await AddUserAsync("alice");
            var bob = await AddUserAsync("bob");
            await _service.ToggleVoteAsync(1, alice.Id);
            await _service.ToggleVoteAsync(1, bob.Id);

            var (voteCount, _) = await _service.GetVoteStatusAsync(1, alice.Id);

            Assert.That(voteCount, Is.EqualTo(2));
        }

        // ── ToggleVoteAsync — adding a vote ───────────────────────────────────

        [Test]
        public async Task ToggleVoteAsync_FirstVote_UserHasVotedIsTrue()
        {
            var user = await AddUserAsync("alice");

            var (_, userHasVoted) = await _service.ToggleVoteAsync(1, user.Id);

            Assert.That(userHasVoted, Is.True);
        }

        [Test]
        public async Task ToggleVoteAsync_FirstVote_VoteCountIsOne()
        {
            var user = await AddUserAsync("alice");

            var (voteCount, _) = await _service.ToggleVoteAsync(1, user.Id);

            Assert.That(voteCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ToggleVoteAsync_FirstVote_AwardsXpToUser()
        {
            var user = await AddUserAsync("alice");

            await _service.ToggleVoteAsync(1, user.Id);

            var points = await _db.UserPoints.FirstAsync(p => p.UserId == user.Id);
            Assert.That(points.CurrentPoints, Is.EqualTo(2));
            Assert.That(points.LifetimePoints, Is.EqualTo(2));
        }

        [Test]
        public async Task ToggleVoteAsync_FirstVote_CreatesUserPointsRecordIfMissing()
        {
            var user = await AddUserAsync("alice");

            await _service.ToggleVoteAsync(1, user.Id);

            var exists = await _db.UserPoints.AnyAsync(p => p.UserId == user.Id);
            Assert.That(exists, Is.True);
        }

        // ── ToggleVoteAsync — removing a vote ─────────────────────────────────

        [Test]
        public async Task ToggleVoteAsync_SecondToggle_RemovesVote()
        {
            var user = await AddUserAsync("alice");
            await _service.ToggleVoteAsync(1, user.Id);

            var (_, userHasVoted) = await _service.ToggleVoteAsync(1, user.Id);

            Assert.That(userHasVoted, Is.False);
        }

        [Test]
        public async Task ToggleVoteAsync_SecondToggle_VoteCountReturnsToZero()
        {
            var user = await AddUserAsync("alice");
            await _service.ToggleVoteAsync(1, user.Id);

            var (voteCount, _) = await _service.ToggleVoteAsync(1, user.Id);

            Assert.That(voteCount, Is.EqualTo(0));
        }

        // ── ToggleVoteAsync — isolation between reports ───────────────────────

        [Test]
        public async Task ToggleVoteAsync_VoteOnDifferentReport_DoesNotAffectOtherReport()
        {
            var user = await AddUserAsync("alice");
            await _service.ToggleVoteAsync(reportId: 1, user.Id);

            var (voteCount, _) = await _service.GetVoteStatusAsync(reportId: 2, user.Id);

            Assert.That(voteCount, Is.EqualTo(0));
        }
    }
}
