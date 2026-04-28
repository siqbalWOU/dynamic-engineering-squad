using System;
using System.Collections.Generic;
using InfrastructureApp.Models;

namespace InfrastructureApp.ViewModels
{
    public class ModerationDashboardViewModel
    {
        public List<FlaggedReportSummary> FlaggedReports { get; set; } = new List<FlaggedReportSummary>();
    }

    public class FlaggedReportSummary
    {
        public int ReportId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReporterId { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int FlagCount { get; set; }
        public List<string> FlagCategories { get; set; } = new List<string>();
        public string? ImageUrl { get; set; }
    }
}
