using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class UpdateStatusServiceTests
    {
        private ApplicationDbContext _db = null!;
        private ReportIssueService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UpdateStatusTest_" + Guid.NewGuid())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _db = new ApplicationDbContext(options);

            _service = new ReportIssueService(
                _db,
                Substitute.For<IReportIssueRepository>(),
                Substitute.For<IWebHostEnvironment>(),
                Substitute.For<InfrastructureApp.Services.ContentModeration.IContentModerationService>(),
                Substitute.For<InfrastructureApp.Services.ImageHashing.IImageHashService>(),
                Substitute.For<InfrastructureApp.Services.ImageSeverity.IImageModerationService>(),
                Substitute.For<InfrastructureApp.Services.ImageSeverity.IImageSeverityEstimationService>(),
                Substitute.For<IHttpContextAccessor>()
            );
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
                UserId = "user-1",
                CreatedAt = DateTime.UtcNow
            };
            _db.ReportIssue.Add(report);
            await _db.SaveChangesAsync();
            return report;
        }

        [Test]
        public async Task UpdateStatusAsync_WhenReportExists_ReturnsTrue()
        {
            var report = await AddReportAsync("Approved");

            var result = await _service.UpdateStatusAsync(report.Id, "Resolved");

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task UpdateStatusAsync_WhenReportExists_UpdatesStatus()
        {
            var report = await AddReportAsync("Approved");

            await _service.UpdateStatusAsync(report.Id, "Resolved");

            var updated = await _db.ReportIssue.FindAsync(report.Id);
            Assert.That(updated!.Status, Is.EqualTo("Resolved"));
        }

        [Test]
        public async Task UpdateStatusAsync_WhenReportNotFound_ReturnsFalse()
        {
            var result = await _service.UpdateStatusAsync(999, "Resolved");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task UpdateStatusAsync_CanTransitionToVerifiedFixed()
        {
            var report = await AddReportAsync("Resolved");

            await _service.UpdateStatusAsync(report.Id, "Verified Fixed");

            var updated = await _db.ReportIssue.FindAsync(report.Id);
            Assert.That(updated!.Status, Is.EqualTo("Verified Fixed"));
        }
    }
}
