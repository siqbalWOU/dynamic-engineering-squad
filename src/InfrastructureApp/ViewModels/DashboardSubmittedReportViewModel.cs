namespace InfrastructureApp.ViewModels;

// SCRUM-137: Represents one report submitted by the logged-in user on their private Dashboard.
public class DashboardSubmittedReportViewModel
{
    // SCRUM-137: Report identifier for stable test targeting and future details-page linking.
    public int Id { get; set; }

    // SCRUM-137: User-submitted report description shown on the Dashboard.
    public string Description { get; set; } = string.Empty;

    // SCRUM-137: Current moderation or workflow status for the submitted report.
    public string Status { get; set; } = string.Empty;

    // SCRUM-137: Date the report was created.
    public DateTime CreatedDate { get; set; }
}
