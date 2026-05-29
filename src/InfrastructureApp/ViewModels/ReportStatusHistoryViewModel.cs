namespace InfrastructureApp.ViewModels;

public class ReportStatusHistoryViewModel
{
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string ChangedByDisplayName { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}
