using InfrastructureApp.Models;

namespace InfrastructureApp.Services;

public interface IStatusHistoryService
{
    Task AddEntryAsync(int reportIssueId, string status, string? userId, string? displayName);
    Task<IReadOnlyList<ReportStatusHistory>> GetHistoryAsync(int reportIssueId);
}
