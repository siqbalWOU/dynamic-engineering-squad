using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class ModerationService : IModerationService
    {
        private readonly ApplicationDbContext _db;

        public ModerationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ModerationDashboardViewModel> GetDashboardViewModelAsync()
        {
            // Get reports that have active (not dismissed) flags
            // We use Include to bring in the flags and then project in-memory to avoid SQLite translation issues with correlated subqueries (SQL APPLY)
            var reportsWithFlags = await _db.ReportIssue
                .Include(r => r.ReportFlags)
                .Where(r => r.ReportFlags.Any(f => !f.IsDismissed))
                .ToListAsync();

            var summaries = reportsWithFlags.Select(r => new FlaggedReportSummary
            {
                ReportId = r.Id,
                Description = r.Description,
                ReporterId = r.UserId,
                CreatedAt = r.CreatedAt,
                ImageUrl = r.ImageUrl,
                FlagCount = r.ReportFlags.Count(f => !f.IsDismissed),
                FlagCategories = r.ReportFlags.Where(f => !f.IsDismissed).Select(f => f.Category).Distinct().ToList()
            })
            .OrderByDescending(s => s.FlagCount)
            .ToList();

            // Populate reporter names
            foreach (var summary in summaries)
            {
                var user = await _db.Users.FindAsync(summary.ReporterId);
                summary.ReporterName = user?.UserName ?? "Unknown";
            }

            return new ModerationDashboardViewModel
            {
                FlaggedReports = summaries
            };
        }

        public async Task<(bool Success, string Message)> DismissReportAsync(int reportId, string moderatorId)
        {
            var report = await _db.ReportIssue.FindAsync(reportId);
            if (report == null) return (false, "Report not found.");

            var activeFlags = await _db.ReportFlags
                .Where(f => f.ReportIssueId == reportId && !f.IsDismissed)
                .ToListAsync();

            if (!activeFlags.Any()) return (false, "No active flags to dismiss.");

            foreach (var flag in activeFlags)
            {
                flag.IsDismissed = true;
            }

            _db.ModerationActionLogs.Add(new ModerationActionLog
            {
                ModeratorId = moderatorId,
                Action = "Dismissed",
                ReportIssueId = reportId,
                TargetContentSnapshot = $"Dismissed flags for report: {report.Description}",
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return (true, "Report flags dismissed successfully.");
        }

        public async Task<(bool Success, string Message)> RemovePostAsync(int reportId, string moderatorId)
        {
            var report = await _db.ReportIssue.FindAsync(reportId);
            if (report == null) return (false, "Report not found.");

            // Create log before deleting
            _db.ModerationActionLogs.Add(new ModerationActionLog
            {
                ModeratorId = moderatorId,
                Action = "Removed",
                ReportIssueId = null, // Set to null because report is being deleted
                TargetContentSnapshot = $"Removed post: {report.Description} (ID: {reportId})",
                Timestamp = DateTime.UtcNow
            });

            _db.ReportIssue.Remove(report);
            await _db.SaveChangesAsync();

            return (true, "Post removed successfully.");
        }
    }
}
