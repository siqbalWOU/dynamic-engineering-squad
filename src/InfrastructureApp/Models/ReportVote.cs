using System;

namespace InfrastructureApp.Models
{
    // Represents a single community upvote on a report.
    // The unique index on (ReportIssueId, UserId) in ApplicationDbContext
    // ensures one vote per user per report.
    public class ReportVote
    {
        public int Id { get; set; }

        // FK to the report being voted on
        public int ReportIssueId { get; set; }

        // FK to the Identity user who cast the vote
        public string UserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
