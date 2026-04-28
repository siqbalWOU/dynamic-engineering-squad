using InfrastructureApp.Services;

namespace InfrastructureApp.ViewModels
{
    // Holds the data needed to display the Dashboard page
    public class DashboardViewModel
    {
        // User's display name
        public string Username { get; set; } = "";

        // User's email address
        public string Email { get; set; } = "";

        // Total number of reports submitted by the user
        public int ReportsSubmitted { get; set; }

        // Current points earned by the user
        public int Points { get; set; }

        //Avatar
        public string? AvatarKey { get; set; }
        public string? AvatarUrl { get; set; }

        public string? PersonalInfoBackgroundUrl { get; set; }
        public string? SelectedDashboardBackgroundKey { get; set; }
        public string? ActivitySummaryBackgroundUrl { get; set; }
        public string? SelectedActivitySummaryBackgroundKey { get; set; }
        public string? PersonalInfoBorderCssClass { get; set; }
        public string? SelectedDashboardBorderKey { get; set; }
        public string? ActivitySummaryBorderCssClass { get; set; }
        public string? SelectedActivitySummaryBorderKey { get; set; }
        public bool IsOwnDashboard { get; set; }
        public IReadOnlyList<DashboardBackgroundOptionViewModel> AvailableDashboardBackgrounds { get; set; }
            = Array.Empty<DashboardBackgroundOptionViewModel>();
        public IReadOnlyList<DashboardBackgroundOptionViewModel> AvailableActivitySummaryBackgrounds { get; set; }
            = Array.Empty<DashboardBackgroundOptionViewModel>();
        public IReadOnlyList<DashboardBorderOptionViewModel> AvailableDashboardBorders { get; set; }
            = Array.Empty<DashboardBorderOptionViewModel>();
        public IReadOnlyList<DashboardBorderOptionViewModel> AvailableActivitySummaryBorders { get; set; }
            = Array.Empty<DashboardBorderOptionViewModel>();

        //Resolves which avatar URL to show - uploaded photo wins over preset
        public string ResolvedAvatarUrl =>
            !string.IsNullOrWhiteSpace(AvatarUrl)
                ?AvatarUrl
                : AvatarCatalog.ToUrl(AvatarKey);

        public bool HasPersonalInfoBackground =>
            !string.IsNullOrWhiteSpace(PersonalInfoBackgroundUrl);

        public bool HasActivitySummaryBackground =>
            !string.IsNullOrWhiteSpace(ActivitySummaryBackgroundUrl);
    }
}
