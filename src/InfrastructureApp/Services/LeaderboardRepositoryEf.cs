using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services;

public class LeaderboardRepositoryEf : ILeaderboardRepository
{
    private readonly ApplicationDbContext _db;

    public LeaderboardRepositoryEf(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<LeaderboardEntry>> GetAllAsync()
    {
        // Keep the EF queries simple so this works consistently with SQLite in Selenium tests.
        var users = await _db.Users
            .Where(u => u.UserName != null)
            .ToListAsync();

        var userPoints = await _db.UserPoints.ToListAsync();
        var pointsByUserId = userPoints
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var results = users.Select(user =>
        {
            pointsByUserId.TryGetValue(user.Id, out var points);

            return new LeaderboardEntry
            {
                UserId = user.UserName!,
                UserPoints = points?.CurrentPoints ?? 0,
                UpdatedAtUtc = points?.LastUpdated ?? default,
                AvatarUrl = !string.IsNullOrWhiteSpace(user.AvatarUrl)
                    ? user.AvatarUrl
                    : AvatarCatalog.ToUrl(user.AvatarKey)
            };
        }).ToList();

        return results.AsReadOnly();
    }
}
