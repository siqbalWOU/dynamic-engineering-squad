using System;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class FlagServiceTests
    {
        private ApplicationDbContext _db = null!;
        private FlagService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("FlagServiceTest_" + Guid.NewGuid())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _db = new ApplicationDbContext(options);
            _service = new FlagService(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task FlagReportAsync_ValidNewFlag_ReturnsSuccess_AndSavesToDb()
        {
            // Arrange
            int reportId = 1;
            string userId = "user-1";
            string category = "Misinformation";

            // Act
            var (success, message) = await _service.FlagReportAsync(reportId, userId, category);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(message, Is.EqualTo("Thank you for your report. Our moderation team will review it shortly."));
                Assert.That(_db.ReportFlags.Count(), Is.EqualTo(1));
                
                var flag = _db.ReportFlags.First();
                Assert.That(flag.ReportIssueId, Is.EqualTo(reportId));
                Assert.That(flag.UserId, Is.EqualTo(userId));
                Assert.That(flag.Category, Is.EqualTo(category));
            });
        }

        [Test]
        public async Task FlagReportAsync_UserAlreadyFlagged_ReturnsFailure_AndDoesNotAddDuplicate()
        {
            // Arrange
            int reportId = 1;
            string userId = "user-1";
            _db.ReportFlags.Add(new ReportFlag { ReportIssueId = reportId, UserId = userId, Category = "Spam" });
            await _db.SaveChangesAsync();

            // Act
            var (success, message) = await _service.FlagReportAsync(reportId, userId, "Misinformation");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(message, Is.EqualTo("You have already flagged this post."));
                Assert.That(_db.ReportFlags.Count(), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task HasUserFlaggedAsync_WhenFlagExists_ReturnsTrue()
        {
            // Arrange
            int reportId = 1;
            string userId = "user-1";
            _db.ReportFlags.Add(new ReportFlag { ReportIssueId = reportId, UserId = userId, Category = "Spam" });
            await _db.SaveChangesAsync();

            // Act
            bool result = await _service.HasUserFlaggedAsync(reportId, userId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task HasUserFlaggedAsync_WhenNoFlagExists_ReturnsFalse()
        {
            // Act
            bool result = await _service.HasUserFlaggedAsync(1, "user-none");

            // Assert
            Assert.That(result, Is.False);
        }
    }
}
