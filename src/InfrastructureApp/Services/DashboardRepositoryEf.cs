using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class DashboardRepositoryEf : IDashboardRepository
    {
        
        private readonly ApplicationDbContext _db; // Database context used for queries
        private readonly UserManager<Users> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardRepositoryEf(ApplicationDbContext db, UserManager<Users> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }


        // Attempt to load a seeded/demo user from the database.
        // If none exists, return placeholder values so the dashboard
        // remains functional until full account integration is enabled.
       public async Task<DashboardViewModel> GetDashboardSummaryAsync()
        {
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext!.User);

            if (currentUser == null)
                return BuildPlaceholderDashboard();

            // Reload fresh from DB to ensure AvatarKey/AvatarUrl are populated
            var freshUser = await _userManager.FindByIdAsync(currentUser.Id);

            if (freshUser == null)
                return BuildPlaceholderDashboard();

            return await BuildDashboardForUserAsync(freshUser);
        }

        // Returns default dashboard values when no user is found
        private static DashboardViewModel BuildPlaceholderDashboard()
        {
            return new DashboardViewModel
            {
                Username = "DemoUser",
                Email = "demo@example.com",
                ReportsSubmitted = 0,
                Points = 0
            };
        }

        // Builds dashboard values using database data
        private async Task<DashboardViewModel> BuildDashboardForUserAsync(Users user)
        {
            // Count how many reports this user submitted
            var reportsSubmitted = await _db.ReportIssue
                .AsNoTracking()
                .CountAsync(r => r.UserId == user.Id);

            // Get user points (if they exist)
            var pointsRow = await _db.UserPoints
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            return new DashboardViewModel
            {
                Username = user.UserName ?? "DemoUser",
                Email = user.Email ?? "demo@example.com",
                ReportsSubmitted = reportsSubmitted,
                Points = pointsRow?.CurrentPoints ?? 0,
                AvatarKey = user.AvatarKey,
                AvatarUrl = user.AvatarUrl
            };
        }
    }
}