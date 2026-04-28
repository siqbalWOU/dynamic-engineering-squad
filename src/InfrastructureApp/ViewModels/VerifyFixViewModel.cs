using InfrastructureApp.Models;

namespace InfrastructureApp.ViewModels
{
    public class VerifyFixViewModel
    {
        public List<VerifyFixItemViewModel> Reports { get; set; } = new();
    }

    public class VerifyFixItemViewModel
    {
        public ReportIssue Report { get; set; } = null!;
        public int VerifyCount { get; set; }
    }
}
