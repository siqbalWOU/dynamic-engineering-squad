using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class VerifyFixService : IVerifyFixService
    {
        private readonly ApplicationDbContext _db;

        private const int PointsForVerifying = 3;
        public const int VerificationThreshold = 3;

        public VerifyFixService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(int verifyCount, bool userHasVerified)> ToggleVerificationAsync(int reportId, string userId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var existing = await _db.ReportVerifications
                    .FirstOrDefaultAsync(v => v.ReportIssueId == reportId && v.UserId == userId);

                bool userHasVerified;

                if (existing != null)
                {
                    _db.ReportVerifications.Remove(existing);
                    userHasVerified = false;
                }
                else
                {
                    _db.ReportVerifications.Add(new ReportVerification
                    {
                        ReportIssueId = reportId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    });

                    var userPoints = await _db.UserPoints.FirstOrDefaultAsync(up => up.UserId == userId);
                    if (userPoints == null)
                    {
                        userPoints = new UserPoints
                        {
                            UserId = userId,
                            CurrentPoints = 0,
                            LifetimePoints = 0,
                            LastUpdated = DateTime.UtcNow
                        };
                        _db.UserPoints.Add(userPoints);
                    }

                    userPoints.CurrentPoints += PointsForVerifying;
                    userPoints.LifetimePoints += PointsForVerifying;
                    userPoints.LastUpdated = DateTime.UtcNow;

                    userHasVerified = true;
                }

                await _db.SaveChangesAsync();

                int verifyCount = await _db.ReportVerifications.CountAsync(v => v.ReportIssueId == reportId);

                // Auto-transition to "Resolved" when threshold is reached so it appears on the verify queue
                if (userHasVerified && verifyCount >= VerificationThreshold)
                {
                    var report = await _db.ReportIssue.FirstOrDefaultAsync(r => r.Id == reportId);
                    if (report != null && report.Status == "Approved")
                    {
                        report.Status = "Resolved";
                        await _db.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();
                return (verifyCount, userHasVerified);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<(int verifyCount, bool userHasVerified)> GetVerifyStatusAsync(int reportId, string? userId)
        {
            int verifyCount = await _db.ReportVerifications.CountAsync(v => v.ReportIssueId == reportId);

            bool userHasVerified = userId != null &&
                await _db.ReportVerifications.AnyAsync(v => v.ReportIssueId == reportId && v.UserId == userId);

            return (verifyCount, userHasVerified);
        }

        public async Task<Dictionary<int, int>> GetVerifyCountsAsync(IEnumerable<int> reportIds)
        {
            var ids = reportIds.ToList();

            return await _db.ReportVerifications
                .Where(v => ids.Contains(v.ReportIssueId))
                .GroupBy(v => v.ReportIssueId)
                .Select(g => new { ReportId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ReportId, x => x.Count);
        }
    }
}
