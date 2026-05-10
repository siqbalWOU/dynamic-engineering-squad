using Microsoft.AspNetCore.Identity;
using InfrastructureApp.Services;


namespace InfrastructureApp.Models
{
    public class Users : IdentityUser
    {
        public string? AvatarKey { get; set; } // e.g. "avatar01", "avatar02"

        public string? AvatarUrl { get; set; }
        public string? SelectedDashboardBackgroundKey { get; set; }
        public string? SelectedActivitySummaryBackgroundKey { get; set; }
        public string? SelectedDashboardBorderKey { get; set; }
        public string? SelectedActivitySummaryBorderKey { get; set; }
        public List<string>? Roles { get; set; }

        public bool IsBanned { get; set; }
        public string? BanReason { get; set; }


        
    }
}
