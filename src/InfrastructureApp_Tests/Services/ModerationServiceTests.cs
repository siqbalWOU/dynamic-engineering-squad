using System;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class ModerationServiceTests
    {
        private ApplicationDbContext _db = null!;
        private ModerationService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ModerationServiceTest_" + Guid.NewGuid())
                .Options;

            _db = new ApplicationDbContext(options);
            _service = new ModerationService(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task GetDashboardViewModelAsync_ReturnsOnlyReportsWithActiveFlags()
        {
            // Arrange
            var report1 = new ReportIssue { Description = "Flagged", Status = "Approved", UserId = "u1" };
            var report2 = new ReportIssue { Description = "Not Flagged", Status = "Approved", UserId = "u2" };
            var report3 = new ReportIssue { Description = "Dismissed Flag", Status = "Approved", UserId = "u3" };
            _db.ReportIssue.AddRange(report1, report2, report3);
            await _db.SaveChangesAsync();

            _db.ReportFlags.Add(new ReportFlag { ReportIssueId = report1.Id, Category = "Spam", UserId = "f1" });
            _db.ReportFlags.Add(new ReportFlag { ReportIssueId = report3.Id, Category = "Spam", UserId = "f2", IsDismissed = true });
            await _db.SaveChangesAsync();

            // Act
            var vm = await _service.GetDashboardViewModelAsync();

            // Assert
            Assert.That(vm.FlaggedReports.Count, Is.EqualTo(1));
            Assert.That(vm.FlaggedReports[0].ReportId, Is.EqualTo(report1.Id));
        }

        [Test]
        public async Task DismissReportAsync_MarksAllFlagsAsDismissed_AndLogsAction()
        {
            // Arrange
            var report = new ReportIssue { Description = "Target", Status = "Approved", UserId = "u1" };
            _db.ReportIssue.Add(report);
            await _db.SaveChangesAsync();

            _db.ReportFlags.Add(new ReportFlag { ReportIssueId = report.Id, Category = "Spam", UserId = "f1" });
            _db.ReportFlags.Add(new ReportFlag { ReportIssueId = report.Id, Category = "Misinfo", UserId = "f2" });
            await _db.SaveChangesAsync();

            // Act
            var (success, _) = await _service.DismissReportAsync(report.Id, "mod-1");

            // Assert
            Assert.That(success, Is.True);
            var flags = await _db.ReportFlags.Where(f => f.ReportIssueId == report.Id).ToListAsync();
            Assert.That(flags.All(f => f.IsDismissed), Is.True);

            var log = await _db.ModerationActionLogs.FirstOrDefaultAsync();
            Assert.That(log, Is.Not.Null);
            Assert.That(log!.Action, Is.EqualTo("Dismissed"));
            Assert.That(log.ModeratorId, Is.EqualTo("mod-1"));
        }

        [Test]
        public async Task RemovePostAsync_DeletesReport_AndLogsAction()
        {
            // Arrange
            var report = new ReportIssue { Description = "Target", Status = "Approved", UserId = "u1" };
            _db.ReportIssue.Add(report);
            await _db.SaveChangesAsync();

            _db.ReportFlags.Add(new ReportFlag { ReportIssueId = report.Id, Category = "Spam", UserId = "f1" });
            await _db.SaveChangesAsync();

            // Act
            var (success, _) = await _service.RemovePostAsync(report.Id, "mod-1");

            // Assert
            Assert.That(success, Is.True);
            Assert.That(await _db.ReportIssue.AnyAsync(r => r.Id == report.Id), Is.False);
            Assert.That(await _db.ReportFlags.AnyAsync(f => f.ReportIssueId == report.Id), Is.False);

            var log = await _db.ModerationActionLogs.FirstOrDefaultAsync();
            Assert.That(log, Is.Not.Null);
            Assert.That(log!.Action, Is.EqualTo("Removed"));
            Assert.That(log.TargetContentSnapshot, Does.Contain("Target"));
        }
    }
}
