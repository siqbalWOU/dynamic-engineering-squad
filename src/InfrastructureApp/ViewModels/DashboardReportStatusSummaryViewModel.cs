namespace InfrastructureApp.ViewModels;

// SCRUM-142: Represents one report status count shown in the Dashboard Activity Summary.
public class DashboardReportStatusSummaryViewModel
{
    // Report status label, such as Approved or Pending.
    public string Status { get; set; } = string.Empty;

    // Number of the logged-in user's reports with this status.
    public int Count { get; set; }
}
