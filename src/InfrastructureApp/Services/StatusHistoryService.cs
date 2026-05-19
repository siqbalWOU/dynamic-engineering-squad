using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services;

public class StatusHistoryService : IStatusHistoryService
{
    private readonly ApplicationDbContext _db;

    public StatusHistoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddEntryAsync(int reportIssueId, string status, string? userId, string? displayName)
    {
        _db.ReportStatusHistory.Add(new ReportStatusHistory
        {
            ReportIssueId = reportIssueId,
            Status = status,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            ChangedByDisplayName = displayName
        });
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ReportStatusHistory>> GetHistoryAsync(int reportIssueId)
    {
        return await _db.ReportStatusHistory
            .Where(h => h.ReportIssueId == reportIssueId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }
}
