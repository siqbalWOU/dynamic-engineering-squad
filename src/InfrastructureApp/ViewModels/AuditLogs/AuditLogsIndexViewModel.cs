using InfrastructureApp.Models;

namespace InfrastructureApp.ViewModels.AuditLogs
{
    public sealed class AuditLogsIndexViewModel
    {
        public IReadOnlyList<AuditLog> Items { get; init; } = Array.Empty<AuditLog>();

        public int CurrentPage { get; init; } = 1;

        public int PageSize { get; init; }

        public int TotalItems { get; init; }

        public int TotalPages { get; init; } = 1;

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
