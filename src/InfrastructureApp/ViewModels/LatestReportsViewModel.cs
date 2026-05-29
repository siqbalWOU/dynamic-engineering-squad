using InfrastructureApp.Models;

namespace InfrastructureApp.ViewModels
{
    // Holds all reports for the Latest Reports page
    public class LatestReportsViewModel
    {
        // Directly use the domain model for display
        public List<ReportIssue> Reports { get; set; } = new();

        // Pagination state for the Latest Reports page. (SCRUM-157)
        public int PageIndex { get; set; } = 1;

        public int TotalPages { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }

        // These preserve the current filter/sort in pagination links.
        public string? SearchQuery { get; set; }

        public string SortOrder { get; set; } = "newest";

        public int PageSize { get; set; } = 10;

        // Builds a short row preview while leaving the full modal description unchanged. (SCRUM-159)
        public string GetDescriptionPreview(string? description)
        {
            const int previewLength = 140;

            if (string.IsNullOrWhiteSpace(description) || description.Length <= previewLength)
            {
                return description ?? string.Empty;
            }

            return description[..previewLength].TrimEnd() + "...";
        }
    }
}

