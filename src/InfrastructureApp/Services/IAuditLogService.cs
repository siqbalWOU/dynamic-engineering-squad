using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.AuditLogs;

namespace InfrastructureApp.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string action, string? userId = null, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AuditLog>> GetLatestAsync(int limit = 100, CancellationToken cancellationToken = default);

        Task<AuditLogsIndexViewModel> GetPageAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    }
}
