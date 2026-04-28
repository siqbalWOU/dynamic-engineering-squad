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
        // Project to an anonymous type first so we can call AvatarCatalog.ToUrl()
        // outside the EF query (it's C# code, not translatable to SQL).
        var raw = await _db.Users
            .Where(u => u.UserName != null)
            .GroupJoin(
                _db.UserPoints,
                u => u.Id,
                p => p.UserId,
                (u, pts) => new { u, pts }
            )
            .Select(x => new
            {
                UserName   = x.u.UserName!,
                AvatarKey  = x.u.AvatarKey,
                AvatarUrl  = x.u.AvatarUrl,
                UserPoints = x.pts.Select(p => p.CurrentPoints).FirstOrDefault(),
                UpdatedAt  = x.pts.Select(p => p.LastUpdated).FirstOrDefault()
            })
            .ToListAsync();

        var results = raw.Select(x => new LeaderboardEntry
        {
            UserId       = x.UserName,
            UserPoints   = x.UserPoints,
            UpdatedAtUtc = x.UpdatedAt,
            AvatarUrl    = !string.IsNullOrWhiteSpace(x.AvatarUrl)
                               ? x.AvatarUrl
                               : AvatarCatalog.ToUrl(x.AvatarKey)
        }).ToList();

        return results.AsReadOnly();
    }
}
