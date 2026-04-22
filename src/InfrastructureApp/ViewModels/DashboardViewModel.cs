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

        //Resolves which avatar URL to show - uploaded photo wins over preset
        public string ResolvedAvatarUrl =>
            !string.IsNullOrWhiteSpace(AvatarUrl)
                ?AvatarUrl
                : AvatarCatalog.ToUrl(AvatarKey);
    }
}