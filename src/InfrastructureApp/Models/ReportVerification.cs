namespace InfrastructureApp.Models
{
    // Represents a single community fix verification on a resolved report.
    // The unique index on (ReportIssueId, UserId) in ApplicationDbContext
    // ensures one verification per user per report.
    public class ReportVerification
    {
        public int Id { get; set; }

        // FK to the report being verified
        public int ReportIssueId { get; set; }

        // FK to the Identity user who submitted the verification
        public string UserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
