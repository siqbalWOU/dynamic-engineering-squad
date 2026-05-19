using InfrastructureApp.Models;

namespace InfrastructureApp.ViewModels
{
    // Holds all reports for the Latest Reports page
    public class LatestReportsViewModel
    {
        // Directly use the domain model for display
        public List<ReportIssue> Reports { get; set; } = new();

        // SCRUM-157: Pagination state for the Latest Reports page.
        public int PageIndex { get; set; } = 1;

        public int TotalPages { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }

        // These preserve the current filter/sort in pagination links.
        public string? SearchQuery { get; set; }

        public string SortOrder { get; set; } = "newest";

        public int PageSize { get; set; } = 10;
    }
}

