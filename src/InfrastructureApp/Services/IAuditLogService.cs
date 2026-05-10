using InfrastructureApp.Models;

namespace InfrastructureApp.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string action, string? userId = null, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AuditLog>> GetLatestAsync(int limit = 100, CancellationToken cancellationToken = default);
    }
}
