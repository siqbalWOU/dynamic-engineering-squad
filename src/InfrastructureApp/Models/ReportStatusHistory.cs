namespace InfrastructureApp.Models;

public class ReportStatusHistory
{
    public int Id { get; set; }
    public int ReportIssueId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? ChangedByUserId { get; set; }
    public string? ChangedByDisplayName { get; set; }

    public ReportIssue ReportIssue { get; set; } = null!;
}
