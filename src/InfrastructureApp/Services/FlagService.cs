using System;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class FlagService : IFlagService
    {
        private readonly ApplicationDbContext _db;

        public FlagService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(bool Success, string Message)> FlagReportAsync(int reportId, string userId, string category)
        {
            var alreadyFlagged = await HasUserFlaggedAsync(reportId, userId);
            if (alreadyFlagged)
            {
                return (false, "You have already flagged this post.");
            }

            var flag = new ReportFlag
            {
                ReportIssueId = reportId,
                UserId = userId,
                Category = category,
                CreatedAt = DateTime.UtcNow
            };

            _db.ReportFlags.Add(flag);
            await _db.SaveChangesAsync();

            return (true, "Thank you for your report. Our moderation team will review it shortly.");
        }

        public async Task<bool> HasUserFlaggedAsync(int reportId, string userId)
        {
            return await _db.ReportFlags.AnyAsync(f => f.ReportIssueId == reportId && f.UserId == userId);
        }
    }
}
