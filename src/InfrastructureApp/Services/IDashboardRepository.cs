using InfrastructureApp.ViewModels;

namespace InfrastructureApp.Services
{
    // Repository interface for loading dashboard data
    public interface IDashboardRepository
    {
        // Returns the data needed to display the Dashboard page
        Task<DashboardViewModel> GetDashboardSummaryAsync();

        // Returns public profile data for any user by username (no email)
        Task<DashboardViewModel?> GetPublicProfileAsync(string username);

        Task<bool> UpdateSelectedDashboardBackgroundAsync(string userId, string? selectedBackgroundKey);

        Task<bool> UpdateSelectedActivitySummaryBackgroundAsync(string userId, string? selectedBackgroundKey);

        Task<bool> UpdateSelectedDashboardBorderAsync(string userId, string? selectedBorderKey);

        Task<bool> UpdateSelectedActivitySummaryBorderAsync(string userId, string? selectedBorderKey);
    }
}
