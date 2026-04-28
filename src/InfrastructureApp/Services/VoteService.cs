using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class VoteService : IVoteService
    {
        private readonly ApplicationDbContext _db;

        // Small XP reward for casting a vote, consistent with the points system
        private const int PointsForVoting = 2;

        public VoteService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(int voteCount, bool userHasVoted)> ToggleVoteAsync(int reportId, string userId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var existing = await _db.ReportVotes
                    .FirstOrDefaultAsync(v => v.ReportIssueId == reportId && v.UserId == userId);

                bool userHasVoted;

                if (existing != null)
                {
                    // Already voted — remove the vote (toggle off)
                    _db.ReportVotes.Remove(existing);
                    userHasVoted = false;
                }
                else
                {
                    // No vote yet — add it and award XP
                    _db.ReportVotes.Add(new ReportVote
                    {
                        ReportIssueId = reportId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Award XP using the same pattern as ReportIssueService
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

                    userPoints.CurrentPoints += PointsForVoting;
                    userPoints.LifetimePoints += PointsForVoting;
                    userPoints.LastUpdated = DateTime.UtcNow;

                    userHasVoted = true;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                int voteCount = await _db.ReportVotes.CountAsync(v => v.ReportIssueId == reportId);
                return (voteCount, userHasVoted);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<(int voteCount, bool userHasVoted)> GetVoteStatusAsync(int reportId, string? userId)
        {
            int voteCount = await _db.ReportVotes.CountAsync(v => v.ReportIssueId == reportId);

            bool userHasVoted = userId != null &&
                await _db.ReportVotes.AnyAsync(v => v.ReportIssueId == reportId && v.UserId == userId);

            return (voteCount, userHasVoted);
        }
    }
}
